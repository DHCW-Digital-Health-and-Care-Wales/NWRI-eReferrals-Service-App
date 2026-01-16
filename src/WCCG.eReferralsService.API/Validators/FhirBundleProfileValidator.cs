using Firely.Fhir.Packages;
using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.Extensions.Options;
using WCCG.eReferralsService.API.Configuration;
using WCCG.eReferralsService.API.Extensions.Logger;

namespace WCCG.eReferralsService.API.Validators
{
    public class FhirBundleProfileValidator : IFhirBundleProfileValidator
    {
        private const string FhirPackagesDirectory = "FhirPackages";

        private readonly ILogger<FhirBundleProfileValidator> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly Validator? _validator;

        public FhirBundleProfileValidator(
            IOptions<FhirBundleProfileValidationConfig> config,
            IHostEnvironment hostEnvironment,
            ILogger<FhirBundleProfileValidator> logger)
        {
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _validator = config.Value.Enabled ? BuildValidator() : null;
        }

        public ProfileValidationOutput Validate(Bundle bundle)
        {
            if (_validator == null)
            {
                _logger.FhirBundleProfileValidationDisabled();
                return new ProfileValidationOutput
                {
                    IsSuccessful = true,
                    Errors = []
                };
            }

            _logger.StartingFhirProfileValidation();
            var result = _validator.Validate(bundle);
            _logger.CompletedFhirProfileValidation(result.Issue.Count);

            return new ProfileValidationOutput
            {
                IsSuccessful = result.Success,
                Errors = result.Issue.Select(x => x.ToString()).ToList()
            };
        }

        private Validator BuildValidator()
        {
            var packagesPath = Path.Combine(_hostEnvironment.ContentRootPath, FhirPackagesDirectory);
            if (!Directory.Exists(packagesPath))
            {
                throw new InvalidOperationException(
                    $"FHIR profile validation is enabled, but the package directory '{packagesPath}' does not exist.");
            }

            var packageFiles = Directory.GetFiles(packagesPath);
            if (packageFiles.Length == 0)
            {
                throw new InvalidOperationException(
                    $"FHIR profile validation is enabled, but no package files were found in '{packagesPath}'.");
            }
            _logger.FhirPackageFilesLoadedForValidation(packageFiles.Length);

            var coreSource = ZipSource.CreateValidationSource();
            var packageSource = new FhirPackageSource(ModelInfo.ModelInspector, packageFiles);

            var multiResolver = new MultiResolver(coreSource, packageSource);
            var cachedMultiResolver = new CachedResolver(multiResolver);
            var snapshotSource = new SnapshotSource(cachedMultiResolver);
            var resolver = new CachedResolver(snapshotSource);

            var terminologyService = new LocalTerminologyService(resolver);
            return new Validator(resolver, terminologyService);
        }
    }
}
