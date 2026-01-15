using Firely.Fhir.Packages;
using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.Extensions.Options;
using WCCG.eReferralsService.API.Configuration;
using WCCG.eReferralsService.API.Extensions;

namespace WCCG.eReferralsService.API.Validators
{
    public class FhirBundleProfileValidator : IFhirBundleProfileValidator
    {
        private const string FhirPackagesDirectory = "FhirPackages";
        private readonly FhirBundleProfileValidationConfig _config;
        private readonly ILogger<FhirBundleProfileValidator> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        private readonly Lazy<Validator> _validator;

        public FhirBundleProfileValidator(
            IOptions<FhirBundleProfileValidationConfig> config,
            IHostEnvironment hostEnvironment,
            ILogger<FhirBundleProfileValidator> logger)
        {
            _config = config.Value;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _validator = new Lazy<Validator>(BuildValidator, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public ProfileValidationOutput Validate(Bundle bundle)
        {
            if (!_config.Enabled)
            {
                _logger.FhirBundleProfileValidationDisabled();
                return new ProfileValidationOutput
                {
                    IsSuccessful = true,
                    Errors = new List<string>()
                };
            }

            _logger.StartingFhirProfileValidation();
            var result = _validator.Value.Validate(bundle);
            _logger.CompletedFhirProfileValidation(result.Issue.Count);

            return new ProfileValidationOutput
            {
                IsSuccessful = result.Success,
                Errors = result.Issue.Select(x => x.ToString()).ToList()
            };
        }

        private Validator BuildValidator()
        {
            var coreSource = ZipSource.CreateValidationSource();

            var packageDirectory = Path.Combine(_hostEnvironment.ContentRootPath, FhirPackagesDirectory);
            if (!Directory.Exists(packageDirectory))
            {
                throw new InvalidOperationException(
                    $"FHIR profile validation is enabled, but the package directory '{packageDirectory}' does not exist.");
            }

            var packageFiles = Directory
                .EnumerateFiles(packageDirectory, "*", SearchOption.TopDirectoryOnly)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (packageFiles.Length == 0)
            {
                throw new InvalidOperationException(
                    $"FHIR profile validation is enabled, but no package files were found in '{packageDirectory}'.");
            }

            var packageFileNames = packageFiles
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrWhiteSpace(n));

            _logger.UsingFhirPackageFiles(string.Join("; ", packageFileNames));
            var packageSource = new FhirPackageSource(
                ModelInfo.ModelInspector,
                packageFiles
            );

            var multiResolver = new MultiResolver(coreSource, packageSource);
            var cachedMultiResolver = new CachedResolver(multiResolver);

            var snapshotSource = new SnapshotSource(cachedMultiResolver);

            IAsyncResourceResolver resolver = new CachedResolver(snapshotSource);

            var terminologyService = new LocalTerminologyService(resolver);
            return new Validator(resolver, terminologyService);
        }
    }
}
