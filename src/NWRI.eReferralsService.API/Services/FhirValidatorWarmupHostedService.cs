using FluentValidation;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Validators;

namespace NWRI.eReferralsService.API.Services;

/// <summary>
/// Background service that initializes the FHIR Validator during application startup.
/// </summary>
public class FhirValidatorWarmupHostedService : IHostedService
{
    private readonly ILogger<FhirValidatorWarmupHostedService> _logger;
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly FhirBundleProfileValidationConfig _validationConfig;

    public FhirValidatorWarmupHostedService(ILogger<FhirValidatorWarmupHostedService> logger,
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
            _logger.LogWarning("FHIR Validator is disabled.");
            return;
        }

        _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
        _logger.LogInformation("║  FHIR VALIDATOR WARMUP SERVICE STARTING                   ║");
        _logger.LogInformation("║  Application will not accept traffic until warmup done    ║");
        _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");

        await _fhirBundleProfileValidator.InitializeAsync(cancellationToken);

        _logger.LogInformation("FHIR Validator warmup complete. Application is ready to accept requests.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("FHIR Validator warmup service stopping.");
        return Task.CompletedTask;
    }
}
