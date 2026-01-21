using Firely.Fhir.Packages;
using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Extensions.Logger;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.API.Validators
{
    public class FhirBundleProfileValidator : IFhirBundleProfileValidator, IDisposable
    {
        private const string FhirPackagesDirectory = "FhirPackages";

        private readonly FhirBundleProfileValidationConfig _config;
        private readonly ILogger<FhirBundleProfileValidator> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        private Validator? _validator;
        private CachedResolver? _cachedResolver;

        // Thread-safety: limit concurrent validations
        private SemaphoreSlim? _semaphore;

        private volatile bool _isInitialized;
        private volatile bool _isReady;

        public FhirBundleProfileValidator(
            IOptions<FhirBundleProfileValidationConfig> config,
            IHostEnvironment hostEnvironment,
            ILogger<FhirBundleProfileValidator> logger)
        {
            _config = config.Value;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public bool IsInitialized => _isInitialized;
        public bool IsReady => _isReady;

        public async Task<ProfileValidationOutput> ValidateAsync(Bundle bundle, CancellationToken cancellationToken = default)
        {
            if (!_config.Enabled)
            {
                _logger.FhirBundleProfileValidationDisabled();
                return new ProfileValidationOutput
                {
                    IsSuccessful = true,
                    Errors = []
                };
            }

            if (!_isInitialized)
            {
                throw new InvalidOperationException("FHIR Validator must be initialized before use.");
            }

            if (!_isReady)
            {
                throw new InvalidOperationException("FHIR Validator is not ready. The service is still warming up.");
            }

            var acquired = await _semaphore!.WaitAsync(TimeSpan.FromSeconds(_config.ValidationTimeoutSeconds), cancellationToken);
            if (!acquired)
            {
                throw new TimeoutException(
                    $"Validation request timed out after {_config.ValidationTimeoutSeconds}s waiting for available slot.");
            }

            try
            {
                _logger.StartingFhirProfileValidation();
                var result = _validator!.Validate(bundle);
                _logger.CompletedFhirProfileValidation(result.Issue.Count);

                return new ProfileValidationOutput
                {
                    IsSuccessful = result.Success,
                    Errors = result.Issue.Select(x => x.ToString()).ToList()
                };
            }
            finally
            {
                _semaphore!.Release();
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _semaphore = new SemaphoreSlim(_config.MaxConcurrentValidations, _config.MaxConcurrentValidations);

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

            _cachedResolver = new CachedResolver(snapshotSource);
            var terminologyService = new LocalTerminologyService(_cachedResolver);

            _validator = new Validator(_cachedResolver, terminologyService);
            _isInitialized = true;

            await PerformWarmupAsync(cancellationToken);

            _isReady = true;
        }

        private async Task PerformWarmupAsync(CancellationToken cancellationToken)
        {
            var warmupBundle = new Bundle
            {
                Type = Bundle.BundleType.Message,
                Entry = new List<Bundle.EntryComponent>
                {
                    new() { Resource = new MessageHeader { Id = "warmup", Event = new Coding("https://fhir.nhs.uk/CodeSystem/message-events-bars", "servicerequest-request") } },
                    new() { Resource = new ServiceRequest { Id = "warmup", Status = RequestStatus.Active, Intent = RequestIntent.Order, Meta = new Meta{Profile = ["https://fhir.nhs.uk/StructureDefinition/BARSServiceRequest-request-referral"]} } },
                    new() { Resource = new Patient { Id = "warmup" } },
                    new() { Resource = new Practitioner { Id = "warmup" } },
                    new() { Resource = new PractitionerRole { Id = "warmup" } },
                    new() { Resource = new Organization { Id = "warmup" } },
                    new() { Resource = new Encounter { Id = "warmup", Status = Encounter.EncounterStatus.InProgress, Class = new Coding("http://warmup", "warmup") } },
                    new() { Resource = new Condition { Id = "warmup" } },
                    new() { Resource = new Observation { Id = "warmup", Status = ObservationStatus.Final, Code = new CodeableConcept() } },
                    new() { Resource = new CarePlan { Id = "warmup", Status = RequestStatus.Active, Intent = CarePlan.CarePlanIntent.Plan } },
                    new() { Resource = new Consent { Id = "warmup", Status = Consent.ConsentState.Active, Scope = new CodeableConcept() } },
                    new() { Resource = new HealthcareService { Id = "warmup" } }
                }
            };

            try
            {
                await Task.Run(() =>
                {
                    var result = _validator!.Validate(warmupBundle);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while validating warmup.");
                // Ignore validation errors during warmup
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _semaphore?.Dispose();
        }
    }
}
