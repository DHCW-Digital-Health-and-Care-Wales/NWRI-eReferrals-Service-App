using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public sealed class ProxyNotImplementedError : BaseFhirHttpError
{
    private readonly string _errorMessage;

    public ProxyNotImplementedError(string errorMessage)
    {
        _errorMessage = errorMessage;
    }

    public override string Code => FhirHttpErrorCodes.ProxyNotImplemented;
    public override string DiagnosticsMessage => $"Not Implemented error: {_errorMessage}";
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.NotSupported;
}
