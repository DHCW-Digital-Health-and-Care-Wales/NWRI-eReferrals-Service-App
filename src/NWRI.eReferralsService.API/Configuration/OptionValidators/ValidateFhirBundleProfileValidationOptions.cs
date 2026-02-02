using Microsoft.Extensions.Options;

namespace NWRI.eReferralsService.API.Configuration.OptionValidators;

[OptionsValidator]
public partial class ValidateFhirBundleProfileValidationOptions : IValidateOptions<FhirBundleProfileValidationConfig>;
