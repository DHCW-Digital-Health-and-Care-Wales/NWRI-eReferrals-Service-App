using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Validators;

namespace NWRI.eReferralsService.API.Services;

/// <summary>
/// Background service that initializes and warmup the FHIR Bundle Profile Validator during startup.
/// This runs BEFORE the application starts accepting requests.
/// </summary>
[ExcludeFromCodeCoverage]
public class FhirBundleProfileValidatorWarmupService : IHostedService
{
    private readonly ILogger<FhirBundleProfileValidatorWarmupService> _logger;
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly FhirBundleProfileValidationConfig _validationConfig;

    public FhirBundleProfileValidatorWarmupService(ILogger<FhirBundleProfileValidatorWarmupService> logger,
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IOptions<FhirBundleProfileValidationConfig> validationConfig)
    {
        _logger = logger;
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _validationConfig = validationConfig.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_validationConfig.Enabled)
        {
            _logger.LogWarning("[Startup] FHIR-Bundle-Profile-Validator is disabled.");
            return;
        }

        _logger.LogInformation("[Startup] FHIR-Bundle-Profile-Validator warmup starting...");
        await _fhirBundleProfileValidator.InitializeAsync(cancellationToken);
        _logger.LogInformation("[Startup] FHIR-Bundle-Profile-Validator warmup complete. Application is ready to accept requests.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
