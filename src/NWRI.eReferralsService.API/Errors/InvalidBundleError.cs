using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;

namespace WCCG.eReferralsService.API.Errors;

public class InvalidBundleError : BaseFhirHttpError
{
    public InvalidBundleError(string validationMessage)
    {
        DiagnosticsMessage = validationMessage;
    }

    public override string Code => FhirHttpErrorCodes.SenderBadRequest;
    public override string DiagnosticsMessage { get; }
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Invalid;
}
