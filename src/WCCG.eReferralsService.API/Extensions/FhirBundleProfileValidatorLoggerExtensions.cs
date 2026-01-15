using System.Diagnostics.CodeAnalysis;

namespace WCCG.eReferralsService.API.Extensions;

[ExcludeFromCodeCoverage]
public static partial class FhirBundleProfileValidatorLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "FHIR bundle profile validation disabled in config. Skipping validation...")]
    public static partial void FhirBundleProfileValidationDisabled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting FHIR profile validation.")]
    public static partial void StartingFhirProfileValidation(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Completed FHIR profile validation. Number of issues: {IssueCount}")]
    public static partial void CompletedFhirProfileValidation(this ILogger logger, int issueCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Using FHIR package files: {PackagePaths}")]
    public static partial void UsingFhirPackageFiles(this ILogger logger, string packagePaths);
}
