using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public sealed class ProxyServerError : BaseFhirHttpError
{
    public ProxyServerError(string diagnosticsMessage)
    {
        DiagnosticsMessage = diagnosticsMessage;
    }

    public override string Code => FhirHttpErrorCodes.ProxyServerError;
    public override string DiagnosticsMessage { get; }
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Exception;
}
