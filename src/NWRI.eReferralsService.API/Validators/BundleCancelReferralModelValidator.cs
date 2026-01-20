using FluentValidation;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Models;
using static WCCG.eReferralsService.API.Constants.ValidationMessages;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace WCCG.eReferralsService.API.Validators;

public class BundleCancelReferralModelValidator : AbstractValidator<BundleCancelReferralModel>
{
    public BundleCancelReferralModelValidator()
    {
       
    }
}
