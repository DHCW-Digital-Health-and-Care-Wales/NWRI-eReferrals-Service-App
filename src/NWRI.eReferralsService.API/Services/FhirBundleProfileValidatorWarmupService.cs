using System.Diagnostics;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Validators;

namespace NWRI.eReferralsService.API.Services;

/// <summary>
/// Background service that initializes and warms up the FHIR Bundle Profile Validator during startup.
/// This runs BEFORE the application starts accepting requests.
/// </summary>
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
            _logger.WarmupFhirBundleProfileValidationDisabled();
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.WarmupFhirBundleProfileValidationStarted();
        await _fhirBundleProfileValidator.InitializeAsync(cancellationToken);

        stopwatch.Stop();

        _logger.WarmupFhirBundleProfileValidationComplete(stopwatch.ElapsedMilliseconds);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
