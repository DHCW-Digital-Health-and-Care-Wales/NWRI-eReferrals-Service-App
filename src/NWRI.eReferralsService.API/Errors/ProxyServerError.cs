using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public sealed class ProxyServerError : BaseFhirHttpError
{
    private readonly string _errorMessage;

    public ProxyServerError(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    public override string Code => FhirHttpErrorCodes.ProxyServerError;
    public override string DiagnosticsMessage => $"Proxy server error: {_errorMessage}";
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Exception;
}
