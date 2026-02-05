using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public sealed class ProxyNotImplementedError : BaseFhirHttpError
{
    internal const string Message =
        "BaRS did not recognize the request. This request has not been implemented within the API.";

    public override string Code => FhirHttpErrorCodes.ProxyNotImplemented;

    public override string DiagnosticsMessage => string.Empty;

    public override OperationOutcome.IssueType IssueType =>
        OperationOutcome.IssueType.NotSupported;
}
