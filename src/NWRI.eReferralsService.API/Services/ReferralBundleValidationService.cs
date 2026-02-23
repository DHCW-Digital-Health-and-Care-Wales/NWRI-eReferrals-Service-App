using FluentValidation;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Validators;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.API.Services;

public class ReferralBundleValidationService
{
    private readonly IValidator<BundleCreateReferralModel> _createBundleValidator;
    private readonly IValidator<BundleCancelReferralModel> _cancelBundleValidator;
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly IEventLogger _eventLogger;

    public ReferralBundleValidationService(
        IValidator<BundleCreateReferralModel> createBundleValidator,
        IValidator<BundleCancelReferralModel> cancelBundleValidator,
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IEventLogger eventLogger)
    {
        _createBundleValidator = createBundleValidator;
        _cancelBundleValidator = cancelBundleValidator;
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _eventLogger = eventLogger;
    }

    public async Task ValidateCreateAsync(Bundle bundle, CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);

        var bundleModel = BundleCreateReferralModel.FromBundle(bundle);
        await ValidateMandatoryDataAsync(bundleModel, _createBundleValidator, cancellationToken);
    }

    public async Task ValidateCancelAsync(Bundle bundle, CancellationToken cancellationToken)
    {
        await ValidateFhirProfileAsync(bundle, cancellationToken);

        var bundleModel = BundleCancelReferralModel.FromBundle(bundle);
        await ValidateMandatoryDataAsync(bundleModel, _cancelBundleValidator, cancellationToken);
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
}
