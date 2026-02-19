using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS;

namespace NWRI.eReferralsService.API.Services;

public interface IWpasCreateReferralRequestMapper
{
    WpasCreateReferralRequest Map(BundleCreateReferralModel createReferralModel);
}
