using Microsoft.Extensions.Options;

namespace WCCG.eReferralsService.API.Configuration.OptionValidators;

[OptionsValidator]
public partial class ValidatePasReferralsApiConfigOptions : IValidateOptions<PasReferralsApiConfig>;
