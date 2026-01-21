using Hl7.Fhir.Model;

namespace NWRI.eReferralsService.API.Errors;

public class InvalidHeaderError : BaseHeaderError
{
    public InvalidHeaderError(string validationMessage) : base(validationMessage)
    {
    }

    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Invalid;
}


