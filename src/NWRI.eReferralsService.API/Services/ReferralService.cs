using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
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

    private readonly IWpasApiClient _wpasApiClient;
    private readonly IValidator<BundleCreateReferralModel> _createBundleValidator;
    private readonly IValidator<BundleCancelReferralModel> _cancelBundleValidator;
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly IValidator<HeadersModel> _headerValidator;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IEventLogger _eventLogger;
    private readonly IRequestFhirHeadersDecoder _requestFhirHeadersDecoder;

    public ReferralService(IWpasApiClient wpasApiClient,
        IValidator<BundleCreateReferralModel> createBundleValidator,
        IValidator<BundleCancelReferralModel> cancelBundleValidator,
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IValidator<HeadersModel> headerValidator,
        JsonSerializerOptions jsonSerializerOptions,
        IEventLogger eventLogger,
        IRequestFhirHeadersDecoder requestFhirHeadersDecoder)
    {
        _wpasApiClient = wpasApiClient;
        _createBundleValidator = createBundleValidator;
        _cancelBundleValidator = cancelBundleValidator;
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _headerValidator = headerValidator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _eventLogger = eventLogger;
        _requestFhirHeadersDecoder = requestFhirHeadersDecoder;
    }

    public async Task<string> ProcessMessageAsync(IHeaderDictionary headers, string requestBody, CancellationToken cancellationToken)
    {
        var processingStopwatch = Stopwatch.StartNew();

        var headersModel = HeadersModel.FromHeaderDictionary(headers);
        await ValidateHeadersAsync(headersModel);

        _eventLogger.Audit(new EventCatalogue.PayloadValidationStarted());
        var bundle = JsonSerializer.Deserialize<Bundle>(requestBody, _jsonSerializerOptions)!;

        var workflowAction = DetermineReferralWorkflowAction(bundle);
        var responseContent = workflowAction switch
        {
            ReferralWorkflowAction.Create => await CreateReferralAsync(requestBody, bundle, cancellationToken),
            ReferralWorkflowAction.Cancel => await CancelReferralAsync(requestBody, bundle, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported workflow action '{workflowAction}'.")
        };

        processingStopwatch.Stop();

        var sourceSystem = _requestFhirHeadersDecoder.GetDecodedSourceSystem(headersModel.RequestingSoftware);
        var userRole = _requestFhirHeadersDecoder.GetDecodedUserRole(headersModel.RequestingPractitioner);

        // TODO: Extract WPAS referral ID from the response
        _eventLogger.Audit(new EventCatalogue.AuditReferralAccepted(sourceSystem, userRole, null,
            processingStopwatch.ElapsedMilliseconds));

        return responseContent;
    }

    private async Task<string> CreateReferralAsync(
        string requestBody,
        Bundle bundle,
        CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);
        await ValidateMandatoryDataAsync(bundle, _createBundleValidator, cancellationToken);

        return await _wpasApiClient.CreateReferralAsync(requestBody, cancellationToken);
    }

    private async Task<string> CancelReferralAsync(
        string requestBody,
        Bundle bundle,
        CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);
        await ValidateMandatoryDataAsync(bundle, _cancelBundleValidator, cancellationToken);

        return await _wpasApiClient.CancelReferralAsync(requestBody, cancellationToken);
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
        _eventLogger.Audit(new EventCatalogue.MandatoryFieldsValidated());
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
}
