using FluentValidation;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Models;
using static NWRI.eReferralsService.API.Constants.ValidationMessages;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Validators;

public class BundleCancelReferralModelValidator : AbstractValidator<BundleCancelReferralModel>
{
    public BundleCancelReferralModelValidator()
    {

    }
}
