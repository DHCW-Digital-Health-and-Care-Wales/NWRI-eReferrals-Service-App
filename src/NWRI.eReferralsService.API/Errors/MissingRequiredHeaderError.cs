using Hl7.Fhir.Model;

namespace WCCG.eReferralsService.API.Errors;

public class MissingRequiredHeaderError : BaseHeaderError
{
    public MissingRequiredHeaderError(string validationMessage) : base(validationMessage)
    {
    }

    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Required;
}
