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
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Mappers;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Models.WPAS.Responses;
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
    private readonly WpasCreateReferralRequestMapper _wpasCreateReferralRequestMapper;
    private readonly WpasJsonSchemaValidator _wpasJsonSchemaValidator;
    private readonly ILogger<ReferralService> _logger;

    public ReferralService(IWpasApiClient wpasApiClient,
        IValidator<BundleCreateReferralModel> createBundleValidator,
        IValidator<BundleCancelReferralModel> cancelBundleValidator,
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IValidator<HeadersModel> headerValidator,
        JsonSerializerOptions jsonSerializerOptions,
        IEventLogger eventLogger,
        IRequestFhirHeadersDecoder requestFhirHeadersDecoder,
        WpasCreateReferralRequestMapper wpasCreateReferralRequestMapper,
        WpasJsonSchemaValidator wpasJsonSchemaValidator,
        ILogger<ReferralService> logger)
    {
        _wpasApiClient = wpasApiClient;
        _createBundleValidator = createBundleValidator;
        _cancelBundleValidator = cancelBundleValidator;
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _headerValidator = headerValidator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _eventLogger = eventLogger;
        _requestFhirHeadersDecoder = requestFhirHeadersDecoder;
        _wpasCreateReferralRequestMapper = wpasCreateReferralRequestMapper;
        _wpasJsonSchemaValidator = wpasJsonSchemaValidator;
        _logger = logger;
    }

    public async Task<string> ProcessMessageAsync(IHeaderDictionary headers, string requestBody, CancellationToken cancellationToken)
    {
        var processingStopwatch = Stopwatch.StartNew();

        var headersModel = HeadersModel.FromHeaderDictionary(headers);
        await ValidateHeadersAsync(headersModel);

        _eventLogger.Audit(new EventCatalogue.PayloadValidationStarted());
        var bundle = JsonSerializer.Deserialize<Bundle>(requestBody, _jsonSerializerOptions)!;

        var workflowAction = DetermineReferralWorkflowAction(bundle);
        WpasReferralResponse? response = workflowAction switch
        {
            ReferralWorkflowAction.Create => await CreateReferralAsync(bundle, cancellationToken),
            ReferralWorkflowAction.Cancel => await CancelReferralAsync(bundle, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported workflow action '{workflowAction}'.")
        };

        processingStopwatch.Stop();

        var sourceSystem = _requestFhirHeadersDecoder.GetDecodedSourceSystem(headersModel.RequestingSoftware);
        var userRole = _requestFhirHeadersDecoder.GetDecodedUserRole(headersModel.RequestingPractitioner);

        _eventLogger.Audit(new EventCatalogue.AuditReferralAccepted(sourceSystem, userRole, response?.ReferralId,
            processingStopwatch.ElapsedMilliseconds));

        // TODO: To be implemented as part of story 565927, for now returning empty string to return 200 OK with empty body to the sender system
        return string.Empty;
    }

    private async Task<WpasCreateReferralResponse> CreateReferralAsync(
        Bundle bundle,
        CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);

        var bundleModel = BundleCreateReferralModel.FromBundle(bundle);
        await ValidateMandatoryDataAsync(bundleModel, _createBundleValidator, cancellationToken);

        var wpasCreateReferralRequest = MapToWpasCreateReferralRequest(bundleModel);
        _eventLogger.Audit(new EventCatalogue.MapFhirToWpas());

        ValidateWpasRequestSchema(wpasCreateReferralRequest);

        var stopwatch = Stopwatch.StartNew();
        var response = await _wpasApiClient.CreateReferralAsync(wpasCreateReferralRequest, cancellationToken);
        _eventLogger.Audit(new EventCatalogue.DataSuccessfullyCommittedToWpas(
            stopwatch.ElapsedMilliseconds,
            response.ReferralId));

        return response;
    }

    private async Task<WpasCancelReferralResponse> CancelReferralAsync(
        Bundle bundle,
        CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);

        var bundleModel = BundleCancelReferralModel.FromBundle(bundle);
        await ValidateMandatoryDataAsync(bundleModel, _cancelBundleValidator, cancellationToken);

        // TODO: Implement mapping of FHIR Bundle to WPAS cancel referral payload
        var wpasCancelReferralRequest = new WpasCancelReferralRequest();

        var stopwatch = Stopwatch.StartNew();
        var response = await _wpasApiClient.CancelReferralAsync(wpasCancelReferralRequest, cancellationToken);
        _eventLogger.Audit(new EventCatalogue.DataSuccessfullyCommittedToWpas(
            stopwatch.ElapsedMilliseconds,
            response.ReferralId));

        return response;
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

    private async Task ValidateMandatoryDataAsync<TModel>(TModel bundleModel, IValidator<TModel> validator, CancellationToken cancellationToken)
       where TModel : IBundleModel<TModel>
    {
        var bundleValidationResult = await validator.ValidateAsync(bundleModel, cancellationToken);
        if (!bundleValidationResult.IsValid)
        {
            throw new BundleValidationException(bundleValidationResult.Errors);
        }
        _eventLogger.Audit(new EventCatalogue.MandatoryFieldsValidated());
    }

    private WpasCreateReferralRequest MapToWpasCreateReferralRequest(BundleCreateReferralModel model)
    {
        try
        {
            return _wpasCreateReferralRequestMapper.Map(model);
        }
        catch (Exception ex)
        {
            _eventLogger.LogError(new EventCatalogue.MapFhirToWpasFailed(), ex);
            throw new BundleValidationException([new ValidationFailure("", "Mapping FHIR Bundle to WPAS payload failed.")]);
        }
    }

    private void ValidateWpasRequestSchema(WpasCreateReferralRequest payload)
    {
        var results = _wpasJsonSchemaValidator.ValidateWpasCreateReferralRequest(payload);
        if (!results.IsValid)
        {
            var errors = results.Details?
                .Where(d => !d.IsValid && d.Errors != null)
                .Select(d => new { d.InstanceLocation, d.Errors });
            _logger.WpasSchemaValidationFailed(JsonSerializer.Serialize(new { IsValid = false, Errors = errors }, _jsonSerializerOptions));
            throw new BundleValidationException([new ValidationFailure("", "WPAS payload JSON schema validation failed.")]);
        }
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
