using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public class CapabilityStatementNotFoundError : BaseFhirHttpError
{
    public CapabilityStatementNotFoundError(string resourcePath, string cause)
    {
        DiagnosticsMessage = $"CapabilityStatement JSON resource was not found. ResourcePath='{resourcePath}'. Cause='{cause}'.";
    }

    public override string Code => FhirHttpErrorCodes.ProxyServerError;
    public override string DiagnosticsMessage { get; }
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Exception;
}
