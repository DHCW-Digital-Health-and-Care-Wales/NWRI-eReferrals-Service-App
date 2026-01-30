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
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Validators
{
    public class FhirBundleProfileValidator : IFhirBundleProfileValidator, IDisposable
    {
        private const string WarmupExampleFilePath = "Swagger/Examples/process-message-payload-and-response.json";
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
        private bool _disposed;

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

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.ValidationTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await _semaphore!.WaitAsync(linkedCts.Token);

                try
                {
                    _logger.StartingFhirProfileValidation();

                    // Avoid blocking the calling thread by running synchronous validation in a separate task
                    var result = await Task.Run(() => _validator!.Validate(bundle), linkedCts.Token);

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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.FhirBundleProfileValidationCancelled();
                throw;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.FhirBundleProfileValidationTimeout(_config.ValidationTimeoutSeconds);
                throw new TimeoutException($"FHIR profile validation timed out after {_config.ValidationTimeoutSeconds} seconds.");
            }
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("FHIR Validator has already been initialized.");
            }

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
            var exampleFilePath = Path.Combine(_hostEnvironment.ContentRootPath, WarmupExampleFilePath);

            if (!File.Exists(exampleFilePath))
            {
                _logger.FhirBundleProfileValidationWarmupSkippedExampleFileNotFound(exampleFilePath);
                return;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(exampleFilePath, cancellationToken);
                var jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();
                var warmupBundle = JsonSerializer.Deserialize<Bundle>(jsonContent, jsonSerializerOptions);

                if (warmupBundle == null)
                {
                    _logger.FhirBundleProfileValidationWarmupSkippedDeserializationFailed(exampleFilePath);
                    return;
                }

                await Task.Run(() =>
                {
                    _validator!.Validate(warmupBundle);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                // Ignore errors during warmup to ensure service starts
                _logger.FhirBundleProfileValidationWarmupSkippedErrorOccurred(ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _semaphore?.Dispose();
            _cachedResolver?.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
