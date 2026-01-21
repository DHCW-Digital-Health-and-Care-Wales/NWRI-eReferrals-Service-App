using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public class UnexpectedError : BaseFhirHttpError
{
    private readonly string _exceptionMessage;

    public UnexpectedError(string exceptionMessage)
    {
        _exceptionMessage = exceptionMessage;
    }

    public override string Code => FhirHttpErrorCodes.ReceiverServerError;
    public override string DiagnosticsMessage => $"Unexpected error: {_exceptionMessage}";
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Transient;
}


