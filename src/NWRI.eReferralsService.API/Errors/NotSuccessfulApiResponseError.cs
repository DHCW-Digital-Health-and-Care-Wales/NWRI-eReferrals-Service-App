using Hl7.Fhir.Model;

namespace WCCG.eReferralsService.API.Errors;

public class NotSuccessfulApiResponseError : BaseFhirHttpError
{
    private readonly string _errorMessage;

    public NotSuccessfulApiResponseError(string fhirErrorCode, string errorMessage)
    {
        Code = fhirErrorCode;
        _errorMessage = errorMessage;
    }

    public override string Code { get; }
    public override string DiagnosticsMessage => $"Receiver error. {_errorMessage}";
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Transient;
}
