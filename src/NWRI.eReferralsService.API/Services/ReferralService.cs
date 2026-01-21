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

    private readonly HttpClient _httpClient;
    private readonly IValidator<BundleCreateReferralModel> _bundleValidator;
    private readonly IValidator<BundleCancelReferralModel> _cancelBundleValidator;
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly IValidator<HeadersModel> _headerValidator;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly PasReferralsApiConfig _pasReferralsApiConfig;

    public ReferralService(HttpClient httpClient,
        IOptions<PasReferralsApiConfig> pasReferralsApiOptions,
        IValidator<BundleCreateReferralModel> bundleValidator,
        IValidator<BundleCancelReferralModel> cancelBundleValidator,
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IValidator<HeadersModel> headerValidator,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _httpClient = httpClient;
        _bundleValidator = bundleValidator;
        _cancelBundleValidator = cancelBundleValidator;
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _headerValidator = headerValidator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _pasReferralsApiConfig = pasReferralsApiOptions.Value;
    }

    public async Task<string> ProcessMessageAsync(IHeaderDictionary headers, string requestBody)
    {
        await ValidateHeadersAsync(headers);
        var bundle = JsonSerializer.Deserialize<Bundle>(requestBody, _jsonSerializerOptions)!;

        var workflowAction = DetermineReferralWorkflowAction(bundle);
        return workflowAction switch
        {
            ReferralWorkflowAction.Create => await CreateReferralAsync(requestBody, bundle),
            ReferralWorkflowAction.Cancel => await CancelReferralAsync(requestBody, bundle),
            _ => throw new InvalidOperationException($"Unsupported workflow action '{workflowAction}'.")
        };
    }

    private static ReferralWorkflowAction DetermineReferralWorkflowAction( Bundle bundle)
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

        throw new BundleValidationException([new ValidationFailure("","Invalid MessageHeader.reason and ServiceRequest.status combination.")]);
    }

    public async Task<string> GetReferralAsync(IHeaderDictionary headers, string? id)
    {
        if (!Guid.TryParse(id, out _))
        {
            throw new RequestParameterValidationException(nameof(id), "Id should be a valid GUID");
        }

        await ValidateHeadersAsync(headers);

        var endpoint = string.Format(CultureInfo.InvariantCulture, _pasReferralsApiConfig.GetReferralEndpoint, id);
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

    private async Task ValidateHeadersAsync(IHeaderDictionary headers)
    {
        var headersModel = HeadersModel.FromHeaderDictionary(headers);

        var headersValidationResult = await _headerValidator.ValidateAsync(headersModel);
        if (!headersValidationResult.IsValid)
        {
            // TODO: Add audit log HeadersValidationFailed
            throw new HeaderValidationException(headersValidationResult.Errors);
        }

        // TODO: Add audit log HeadersValidationSucceeded
    }

    private void ValidateFhirProfile(Bundle bundle)
    {
        var validationOutput = _fhirBundleProfileValidator.Validate(bundle);
        if (!validationOutput.IsSuccessful)
        {
            // TODO: Add audit log FhirProfileValidationFailed
            throw new FhirProfileValidationException(validationOutput.Errors!);
        }

        // TODO: Add audit log FhirProfileValidationSucceeded
    }

    private async Task ValidateMandatoryDataAsync(Bundle bundle)
    {
        var bundleModel = BundleCreateReferralModel.FromBundle(bundle);

        var bundleValidationResult = await _bundleValidator.ValidateAsync(bundleModel);
        if (!bundleValidationResult.IsValid)
        {
            // TODO: Add audit log MandatoryDataValidationFailed
            throw new BundleValidationException(bundleValidationResult.Errors);
        }

        // TODO: Add audit log MandatoryDataValidationSucceeded
    }

    private async Task ValidateMandatoryCancelDataAsync(Bundle bundle)
    {
        var bundleModel = BundleCancelReferralModel.FromBundle(bundle);

        var bundleValidationResult = await _cancelBundleValidator.ValidateAsync(bundleModel);
        if (!bundleValidationResult.IsValid)
        {
            // TODO: Add audit log MandatoryCancelDataFailed
            throw new BundleValidationException(bundleValidationResult.Errors);
        }

        // TODO: Add audit log MandatoryCancelDataSucceeded
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

    private async Task<string> CreateReferralAsync(string requestBody, Bundle bundle)
    {
        ValidateFhirProfile(bundle);
        await ValidateMandatoryDataAsync(bundle);

        using var response = await _httpClient.PostAsync(_pasReferralsApiConfig.CreateReferralEndpoint,
            new StringContent(requestBody, new MediaTypeHeaderValue(FhirConstants.FhirMediaType)));

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        throw await GetNotSuccessfulApiCallExceptionAsync(response);
    }

    private async Task<string> CancelReferralAsync(string requestBody, Bundle bundle)
    {
        ValidateFhirProfile(bundle);
        await ValidateMandatoryCancelDataAsync(bundle);

        using var response = await _httpClient.PostAsync(_pasReferralsApiConfig.CreateReferralEndpoint,
            new StringContent(requestBody, new MediaTypeHeaderValue(FhirConstants.FhirMediaType)));

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        throw await GetNotSuccessfulApiCallExceptionAsync(response);
    }
}
