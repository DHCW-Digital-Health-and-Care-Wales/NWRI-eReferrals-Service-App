using WCCG.eReferralsService.API.Constants;

namespace WCCG.eReferralsService.API.Errors;

public abstract class BaseHeaderError : BaseFhirHttpError
{
    protected BaseHeaderError(string validationMessage)
    {
        DiagnosticsMessage = validationMessage;
    }

    public override string Code => FhirHttpErrorCodes.SenderBadRequest;
    public override string DiagnosticsMessage { get; }
}
