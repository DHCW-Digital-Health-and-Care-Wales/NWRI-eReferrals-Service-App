using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public sealed class ProxyServerError : BaseFhirHttpError
{
    internal const string Message = "Proxy Error.";

    public override string Code => FhirHttpErrorCodes.ProxyServerError;

    public override string DiagnosticsMessage => string.Empty;

    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Exception;
}
