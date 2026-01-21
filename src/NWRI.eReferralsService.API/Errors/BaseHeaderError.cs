using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Errors;

public abstract class BaseHeaderError : BaseFhirHttpError
{
    protected BaseHeaderError(string validationMessage)
    {
        DiagnosticsMessage = validationMessage;
    }

    public override string Code => FhirHttpErrorCodes.SenderBadRequest;
    public override string DiagnosticsMessage { get; }
}


