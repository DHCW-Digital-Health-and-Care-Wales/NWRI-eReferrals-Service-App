using Microsoft.Extensions.Diagnostics.HealthChecks;
using NWRI.eReferralsService.API.Validators;

namespace NWRI.eReferralsService.API.HealthChecks;

public class FhirBundleProfileValidatorHealthCheck : IHealthCheck
{
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;

    public FhirBundleProfileValidatorHealthCheck(IFhirBundleProfileValidator fhirBundleProfileValidator)
    {
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_fhirBundleProfileValidator.IsInitialized)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("FHIR Validator failed to initialize."));
        }

        if (_fhirBundleProfileValidator.IsReady)
        {
            return Task.FromResult(HealthCheckResult.Healthy("FHIR Bundle Profile Validator is ready."));
        }

        return Task.FromResult(HealthCheckResult.Degraded("FHIR Bundle Profile Validator is warming up."));
    }
}
