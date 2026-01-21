using Hl7.Fhir.Model;

namespace NWRI.eReferralsService.API.Errors;

public class MissingRequiredHeaderError : BaseHeaderError
{
    public MissingRequiredHeaderError(string validationMessage) : base(validationMessage)
    {
    }

    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Required;
}
