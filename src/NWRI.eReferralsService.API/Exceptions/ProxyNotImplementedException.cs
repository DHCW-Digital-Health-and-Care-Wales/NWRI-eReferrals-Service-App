using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public sealed class ProxyNotImplementedException : BaseFhirException
{
    public override IEnumerable<BaseFhirHttpError> Errors { get; } =
        [new ProxyNotImplementedError()];

    public override string Message => ProxyNotImplementedError.Message;

    public ProxyNotImplementedException()
    {
    }
}
