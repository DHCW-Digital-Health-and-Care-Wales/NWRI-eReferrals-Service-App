using System.Diagnostics.CodeAnalysis;

namespace NWRI.eReferralsService.API.Extensions.Logger;

[ExcludeFromCodeCoverage]
public static partial class FhirBundleProfileValidatorLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "FHIR bundle profile validation disabled in config. Skipping validation...")]
    public static partial void FhirBundleProfileValidationDisabled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting FHIR profile validation.")]
    public static partial void StartingFhirProfileValidation(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed FHIR profile validation. Number of issues: {IssueCount}.")]
    public static partial void CompletedFhirProfileValidation(this ILogger logger, int issueCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "FHIR package files loaded for validation: {PackagesNumber}.")]
    public static partial void FhirPackageFilesLoadedForValidation(this ILogger logger, int packagesNumber);

    [LoggerMessage(Level = LogLevel.Error, Message = "FHIR profile validation was canceled.")]
    public static partial void FhirBundleProfileValidationCancelled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "FHIR profile validation timed out after {timeoutSeconds} seconds.")]
    public static partial void FhirBundleProfileValidationTimeout(this ILogger logger, int timeoutSeconds);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Warmup skipped: Example file not found at '{exampleFilePath}'.")]
    public static partial void FhirBundleProfileValidationWarmupSkippedExampleFileNotFound(this ILogger logger, string exampleFilePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Warmup skipped: Failed to deserialize Bundle from '{exampleFilePath}'.")]
    public static partial void FhirBundleProfileValidationWarmupSkippedDeserializationFailed(this ILogger logger, string exampleFilePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Warmup skipped: An error occurred while warming up the validator.")]
    public static partial void FhirBundleProfileValidationWarmupSkippedErrorOccurred(this ILogger logger, Exception exception);
}
