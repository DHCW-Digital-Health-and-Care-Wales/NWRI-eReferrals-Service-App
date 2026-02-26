using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public sealed class CapabilityStatementError : BaseFhirHttpError
{
    public CapabilityStatementError(string resourcePath, string cause)
    {
        DiagnosticsMessage = $"CapabilityStatement resource unavailable. ResourcePath='{resourcePath}'. Cause='{cause}'.";
    }

    public override string Code => FhirHttpErrorCodes.ProxyServerError;
    public override string DiagnosticsMessage { get; }
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Exception;
}
