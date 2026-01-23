using System.ComponentModel.DataAnnotations;

namespace NWRI.eReferralsService.API.Configuration;

public class FhirBundleProfileValidationConfig
{
    public const string SectionName = "FhirBundleProfileValidation";

    [Required]
    public bool Enabled { get; set; } = true;

    [Required]
    [Range(1, 100)]
    public int MaxConcurrentValidations { get; set; } = Environment.ProcessorCount;

    [Required]
    public int ValidationTimeoutSeconds { get; set; } = 10;
}
