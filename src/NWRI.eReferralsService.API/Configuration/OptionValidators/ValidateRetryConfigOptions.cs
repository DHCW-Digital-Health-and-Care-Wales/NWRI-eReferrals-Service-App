using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration.Resilience;

namespace NWRI.eReferralsService.API.Configuration.OptionValidators;

[OptionsValidator]
public partial class ValidateRetryConfigOptions : IValidateOptions<RetryConfig>;
