using Microsoft.Extensions.Options;
using WCCG.eReferralsService.API.Configuration.Resilience;

namespace WCCG.eReferralsService.API.Configuration.OptionValidators;

[OptionsValidator]
public partial class ValidateResilienceConfigOptions : IValidateOptions<ResilienceConfig>;
