namespace WCCG.eReferralsService.API.Configuration;

public class FhirBundleProfileValidationConfig
{
    public const string SectionName = "FhirBundleProfileValidation";

    public bool Enabled { get; set; } = true;
}
