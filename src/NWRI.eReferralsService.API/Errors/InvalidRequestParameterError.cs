using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;

namespace WCCG.eReferralsService.API.Errors;

public class InvalidRequestParameterError : BaseFhirHttpError
{
    public InvalidRequestParameterError(string parameterName, string validationErrorMessage)
    {
        DiagnosticsMessage = $"Request parameter validation failure. Parameter name: {parameterName}, Error: {validationErrorMessage}.";
    }

    public override string Code => FhirHttpErrorCodes.SenderBadRequest;
    public override string DiagnosticsMessage { get; }
    public override OperationOutcome.IssueType IssueType => OperationOutcome.IssueType.Invalid;
}
