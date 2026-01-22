using Hl7.Fhir.Model;

namespace NWRI.eReferralsService.API.Validators;

public interface IFhirBundleProfileValidator
{
    ProfileValidationOutput Validate(Bundle bundle);
}
