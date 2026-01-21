using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public class ApiCallError : BaseFhirHttpError
{
    private readonly string _errorMessage;

    public ApiCallError(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    public override string Code => FhirHttpErrorCodes.ReceiverUnavailable;
    public override string DiagnosticsMessage => $"Unexpected receiver error: {_errorMessage}";
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Transient;
}


