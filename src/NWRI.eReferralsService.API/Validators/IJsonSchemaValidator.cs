using Json.Schema;
using NWRI.eReferralsService.API.Models.WPAS;

namespace NWRI.eReferralsService.API.Validators;

public interface IJsonSchemaValidator
{
    EvaluationResults Validate(WpasCreateReferralRequest wpasCreateReferralRequest, string jsonSchemaPath);
}
