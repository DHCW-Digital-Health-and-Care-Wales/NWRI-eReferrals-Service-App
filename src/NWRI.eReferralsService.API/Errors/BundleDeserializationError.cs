using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;

namespace WCCG.eReferralsService.API.Errors;

public class BundleDeserializationError : BaseFhirHttpError
{
    private readonly string _exceptionMessage;

    public BundleDeserializationError(string exceptionMessage)
    {
        _exceptionMessage = exceptionMessage;
    }

    public override string Code => FhirHttpErrorCodes.SenderBadRequest;
    public override string DiagnosticsMessage => $"Bundle deserialization error: {_exceptionMessage}";
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Structure;
}
