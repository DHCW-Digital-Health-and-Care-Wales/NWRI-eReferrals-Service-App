using FluentValidation;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Validators;
using Task = System.Threading.Tasks.Task;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Services;

public class ReferralBundleValidationService
{
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly IEventLogger _eventLogger;
    private readonly IServiceProvider _serviceProvider;

    public ReferralBundleValidationService(
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IEventLogger eventLogger,
        IServiceProvider serviceProvider)
    {
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _eventLogger = eventLogger;
        _serviceProvider = serviceProvider;
    }

    public async Task ValidateFhirProfileAsync(Bundle bundle, CancellationToken cancellationToken)
    {
        var validationOutput = await _fhirBundleProfileValidator.ValidateAsync(bundle, cancellationToken);
        if (!validationOutput.IsSuccessful)
        {
            throw new FhirProfileValidationException(validationOutput.Errors!);
        }

        _eventLogger.Audit(new EventCatalogue.FhirSchemaValidated());
    }

    public async Task ValidateMandatoryDataAsync<TModel>(TModel bundleModel, CancellationToken cancellationToken)
        where TModel : class
    {
        var validator = _serviceProvider.GetRequiredService<IValidator<TModel>>();

        var bundleValidationResult = await validator.ValidateAsync(bundleModel, cancellationToken);
        if (!bundleValidationResult.IsValid)
        {
            throw new BundleValidationException(bundleValidationResult.Errors);
        }

        _eventLogger.Audit(new EventCatalogue.MandatoryFieldsValidated());
    }
}
