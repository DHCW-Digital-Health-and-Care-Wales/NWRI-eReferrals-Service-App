using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public class CapabilityStatementLoadError : BaseFhirHttpError
{
    public CapabilityStatementLoadError(string resourcePath, string cause)
    {
        DiagnosticsMessage = $"CapabilityStatement JSON resource could not be loaded. ResourcePath='{resourcePath}'. Cause='{cause}'.";
    }

    public override string Code => FhirHttpErrorCodes.ProxyServerError;
    public override string DiagnosticsMessage { get; }
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Exception;
}
