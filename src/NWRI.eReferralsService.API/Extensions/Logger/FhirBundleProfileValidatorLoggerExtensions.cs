using System.Diagnostics.CodeAnalysis;

namespace NWRI.eReferralsService.API.Extensions.Logger;

[ExcludeFromCodeCoverage]
public static partial class FhirBundleProfileValidatorLoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "FHIR bundle profile validation disabled in config. Skipping validation...")]
    public static partial void FhirBundleProfileValidationDisabled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting FHIR profile validation.")]
    public static partial void StartingFhirProfileValidation(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Completed FHIR profile validation. Number of issues: {IssueCount}")]
    public static partial void CompletedFhirProfileValidation(this ILogger logger, int issueCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "FHIR package files loaded for validation: {PackagesNumber}")]
    public static partial void FhirPackageFilesLoadedForValidation(this ILogger logger, int packagesNumber);
}


