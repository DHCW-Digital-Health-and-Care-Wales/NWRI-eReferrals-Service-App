using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS;

namespace NWRI.eReferralsService.API.Services;

public interface IWpasOutpatientReferralMapper
{
    WpasCreateReferralRequest Map(BundleCreateReferralModel createReferralModel);
}
