using System.Text.Json;
using Firely.Fhir.Packages;
using Firely.Fhir.Validation;
using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Extensions;
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
                throw new TimeoutException($"Profile Bundle Validation timed out after {_config.ValidationTimeoutSeconds}s waiting for available slot.");
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
            var exampleFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "Swagger", "Examples", "process-message-payload&response.json");

            if (!File.Exists(exampleFilePath))
            {
                _logger.LogWarning("Warmup skipped: Example file not found at '{ExampleFilePath}'", exampleFilePath);
                return;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(exampleFilePath, cancellationToken);
                var jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();
                var warmupBundle = JsonSerializer.Deserialize<Bundle>(jsonContent, jsonSerializerOptions);

                if (warmupBundle == null)
                {
                    _logger.LogWarning("Warmup skipped: Failed to deserialize Bundle from '{ExampleFilePath}'", exampleFilePath);
                    return;
                }

                await Task.Run(() =>
                {
                    _validator!.Validate(warmupBundle);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                // Ignore validation errors during warmup
                _logger.LogError(ex, "An error occurred while warmup validator.");
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
            _cachedResolver?.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
