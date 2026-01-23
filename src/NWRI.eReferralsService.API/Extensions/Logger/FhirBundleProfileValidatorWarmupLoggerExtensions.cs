using System.Diagnostics.CodeAnalysis;

namespace NWRI.eReferralsService.API.Extensions.Logger;

[ExcludeFromCodeCoverage]
public static partial class FhirBundleProfileValidatorWarmupLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "[Startup] FHIR-Bundle-Profile-Validator is disabled.")]
    public static partial void WarmupFhirBundleProfileValidationDisabled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Startup] FHIR-Bundle-Profile-Validator warmup starting...")]
    public static partial void WarmupFhirBundleProfileValidationStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Startup] FHIR-Bundle-Profile-Validator warmup complete. Application is ready to accept requests. Duration: {elapsedMilliseconds}ms")]
    public static partial void WarmupFhirBundleProfileValidationComplete(this ILogger logger, long elapsedMilliseconds);
}
