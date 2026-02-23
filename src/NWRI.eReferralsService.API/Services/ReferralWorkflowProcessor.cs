using System.Diagnostics;
using System.Text.Json;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Mappers;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Models.WPAS.Responses;
using NWRI.eReferralsService.API.Validators;

namespace NWRI.eReferralsService.API.Services;

public class ReferralWorkflowProcessor
{
    private readonly ReferralBundleValidationService _referralBundleValidationService;
    private readonly WpasCreateReferralRequestMapper _wpasCreateReferralRequestMapper;
    private readonly WpasJsonSchemaValidator _wpasJsonSchemaValidator;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IWpasApiClient _wpasApiClient;
    private readonly IEventLogger _eventLogger;

    public ReferralWorkflowProcessor(
        ReferralBundleValidationService referralBundleValidationService,
        WpasCreateReferralRequestMapper wpasCreateReferralRequestMapper,
        WpasJsonSchemaValidator wpasJsonSchemaValidator,
        JsonSerializerOptions jsonSerializerOptions,
        IWpasApiClient wpasApiClient,
        IEventLogger eventLogger)
    {
        _referralBundleValidationService = referralBundleValidationService;
        _wpasCreateReferralRequestMapper = wpasCreateReferralRequestMapper;
        _wpasJsonSchemaValidator = wpasJsonSchemaValidator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _wpasApiClient = wpasApiClient;
        _eventLogger = eventLogger;
    }

    public async Task<WpasCreateReferralResponse> ProcessCreateAsync(Bundle bundle, CancellationToken cancellationToken)
    {
        await _referralBundleValidationService.ValidateCreateAsync(bundle, cancellationToken);

        var bundleModel = BundleCreateReferralModel.FromBundle(bundle);

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

    public async Task<WpasCancelReferralResponse> ProcessCancelAsync(Bundle bundle, CancellationToken cancellationToken)
    {
        await _referralBundleValidationService.ValidateCancelAsync(bundle, cancellationToken);

        // TODO: Mapping from FHIR Bundle to WPAS to be implemented as part of story 565342
        var wpasCancelReferralRequest = new WpasCancelReferralRequest();

        var stopwatch = Stopwatch.StartNew();
        var response = await _wpasApiClient.CancelReferralAsync(wpasCancelReferralRequest, cancellationToken);
        _eventLogger.Audit(new EventCatalogue.DataSuccessfullyCommittedToWpas(
            stopwatch.ElapsedMilliseconds,
            response.ReferralId));

        return response;
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
            var details = JsonSerializer.Serialize(new { IsValid = false, Errors = errors }, _jsonSerializerOptions);
            throw new WpasSchemaValidationException(details);
        }
    }
}
