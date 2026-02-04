using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Validators;
using Task = System.Threading.Tasks.Task;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Services;

public class ReferralService : IReferralService
{
    private enum ReferralWorkflowAction
    {
        Create,
        Cancel
    }

    private sealed record WpasCallSuccessResult(string Content, long ExecutionTimeMs);

    private readonly HttpClient _httpClient;
    private readonly IValidator<BundleCreateReferralModel> _createBundleValidator;
    private readonly IValidator<BundleCancelReferralModel> _cancelBundleValidator;
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly IValidator<HeadersModel> _headerValidator;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly WpasApiConfig _wpasApiConfig;
    private readonly IEventLogger _eventLogger;
    private readonly IRequestHeadersDecoder _requestHeadersDecoder;

    public ReferralService(HttpClient httpClient,
        IOptions<WpasApiConfig> wpasApiOptions,
        IValidator<BundleCreateReferralModel> createBundleValidator,
        IValidator<BundleCancelReferralModel> cancelBundleValidator,
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IValidator<HeadersModel> headerValidator,
        JsonSerializerOptions jsonSerializerOptions,
        IEventLogger eventLogger,
        IRequestHeadersDecoder requestHeadersDecoder)
    {
        _httpClient = httpClient;
        _createBundleValidator = createBundleValidator;
        _cancelBundleValidator = cancelBundleValidator;
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _headerValidator = headerValidator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _wpasApiConfig = wpasApiOptions.Value;
        _eventLogger = eventLogger;
        _requestHeadersDecoder = requestHeadersDecoder;
    }

    public async Task<string> ProcessMessageAsync(IHeaderDictionary headers, string requestBody, CancellationToken cancellationToken)
    {
        var processingStopwatch = Stopwatch.StartNew();

        var headersModel = HeadersModel.FromHeaderDictionary(headers);
        await ValidateHeadersAsync(headersModel);

        _eventLogger.Audit(new EventCatalogue.PayloadValidationStarted());
        var bundle = JsonSerializer.Deserialize<Bundle>(requestBody, _jsonSerializerOptions)!;

        var workflowAction = DetermineReferralWorkflowAction(bundle);
        var result = workflowAction switch
        {
            ReferralWorkflowAction.Create => await CreateReferralAsync(requestBody, bundle, cancellationToken),
            ReferralWorkflowAction.Cancel => await CancelReferralAsync(requestBody, bundle, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported workflow action '{workflowAction}'.")
        };

        processingStopwatch.Stop();

        // TODO: Extract WPAS referral ID from the response
        _eventLogger.Audit(new EventCatalogue.DataSuccessfullyCommittedToWpas(result.ExecutionTimeMs, null));

        var sourceSystem = _requestHeadersDecoder.GetDecodedSourceSystem(headersModel.RequestingSoftware);
        var userRole = _requestHeadersDecoder.GetDecodedUserRole(headersModel.RequestingPractitioner);

        // TODO: Extract WPAS referral ID from the response
        _eventLogger.Audit(new EventCatalogue.AuditReferralAccepted(sourceSystem, userRole, null,
            processingStopwatch.ElapsedMilliseconds));

        return result.Content;
    }

    private static ReferralWorkflowAction DetermineReferralWorkflowAction(Bundle bundle)
    {
        var reasonCode = GetMessageReasonCode(bundle);
        if (reasonCode is null)
        {
            throw new RequestParameterValidationException("MessageHeader.reason", "MessageHeader.reason.coding.code is required");
        }

        var serviceRequestStatus = GetServiceRequestStatus(bundle);
        if (serviceRequestStatus is null)
        {
            throw new RequestParameterValidationException("ServiceRequest.status", "ServiceRequest.status is required");
        }

        if (reasonCode == FhirConstants.BarsMessageReasonNew && serviceRequestStatus == RequestStatus.Active)
        {
            return ReferralWorkflowAction.Create;
        }

        if (reasonCode == FhirConstants.BarsMessageReasonUpdate &&
            serviceRequestStatus is RequestStatus.Revoked or RequestStatus.EnteredInError)
        {
            return ReferralWorkflowAction.Cancel;
        }

        throw new BundleValidationException([new ValidationFailure("", "Invalid MessageHeader.reason and ServiceRequest.status combination.")]);
    }

    public async Task<string> GetReferralAsync(IHeaderDictionary headers, string? id)
    {
        if (!Guid.TryParse(id, out _))
        {
            throw new RequestParameterValidationException(nameof(id), "Id should be a valid GUID");
        }

        var headersModel = HeadersModel.FromHeaderDictionary(headers);
        await ValidateHeadersAsync(headersModel);

        var endpoint = string.Format(CultureInfo.InvariantCulture, _wpasApiConfig.GetReferralEndpoint, id);
        using var response = await _httpClient.GetAsync(endpoint);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        throw await GetNotSuccessfulApiCallExceptionAsync(response);
    }

    private static async Task<Exception> GetNotSuccessfulApiCallExceptionAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content);
            return new NotSuccessfulApiCallException(response.StatusCode, problemDetails!);
        }
        catch (JsonException)
        {
            return new NotSuccessfulApiCallException(response.StatusCode, content);
        }
    }

    private async Task ValidateHeadersAsync(HeadersModel headersModel)
    {
        var headersValidationResult = await _headerValidator.ValidateAsync(headersModel);
        if (!headersValidationResult.IsValid)
        {
            throw new HeaderValidationException(headersValidationResult.Errors);
        }
        _eventLogger.Audit(new EventCatalogue.HeadersValidated());
    }

    private async Task ValidateFhirProfileAsync(Bundle bundle, CancellationToken cancellationToken)
    {
        var validationOutput = await _fhirBundleProfileValidator.ValidateAsync(bundle, cancellationToken);
        if (!validationOutput.IsSuccessful)
        {
            throw new FhirProfileValidationException(validationOutput.Errors!);
        }
        _eventLogger.Audit(new EventCatalogue.FhirSchemaValidated());
    }

    private async Task ValidateMandatoryDataAsync<TModel>(Bundle bundle, IValidator<TModel> validator, CancellationToken cancellationToken)
       where TModel : IBundleModel<TModel>
    {
        var bundleModel = TModel.FromBundle(bundle);

        var bundleValidationResult = await validator.ValidateAsync(bundleModel, cancellationToken);
        if (!bundleValidationResult.IsValid)
        {
            throw new BundleValidationException(bundleValidationResult.Errors);
        }
        _eventLogger.Audit(new EventCatalogue.FhirMappedToWpasPayload());
    }

    private static string? GetMessageReasonCode(Bundle bundle)
    {
        var messageHeader = bundle.ResourceByType<MessageHeader>();
        return messageHeader?.Reason?.Coding
            .FirstOrDefault(c => string.Equals(c.System, FhirConstants.BarsMessageReasonSystem, StringComparison.OrdinalIgnoreCase))
            ?.Code;
    }

    private static RequestStatus? GetServiceRequestStatus(Bundle bundle)
    {
        var serviceRequest = bundle.ResourceByType<ServiceRequest>();
        return serviceRequest?.Status;
    }

    private async Task<WpasCallSuccessResult> CreateReferralAsync(
        string requestBody,
        Bundle bundle,
        CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);
        await ValidateMandatoryDataAsync(bundle, _createBundleValidator, cancellationToken);

        var callToWpasStopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsync(_wpasApiConfig.CreateReferralEndpoint,
            new StringContent(requestBody, new MediaTypeHeaderValue(FhirConstants.FhirMediaType)), cancellationToken);

        callToWpasStopwatch.Stop();

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new WpasCallSuccessResult(content, callToWpasStopwatch.ElapsedMilliseconds);
        }

        throw await GetNotSuccessfulApiCallExceptionAsync(response);
    }

    private async Task<WpasCallSuccessResult> CancelReferralAsync(
        string requestBody,
        Bundle bundle,
        CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);
        await ValidateMandatoryDataAsync(bundle, _cancelBundleValidator, cancellationToken);

        var callToWpasStopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsync(_wpasApiConfig.CancelReferralEndpoint,
            new StringContent(requestBody, new MediaTypeHeaderValue(FhirConstants.FhirMediaType)), cancellationToken);

        callToWpasStopwatch.Stop();

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new WpasCallSuccessResult(content, callToWpasStopwatch.ElapsedMilliseconds);
        }

        throw await GetNotSuccessfulApiCallExceptionAsync(response);
    }
}
