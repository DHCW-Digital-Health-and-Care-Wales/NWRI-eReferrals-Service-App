using Json.Schema;
using NWRI.eReferralsService.API.Models.WPAS;

namespace NWRI.eReferralsService.API.Validators;

public interface IWpasJsonSchemaValidator
{
    EvaluationResults ValidateWpasCreateReferralRequest(WpasCreateReferralRequest wpasCreateReferralRequest);
}
