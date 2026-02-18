using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public sealed class CapabilityStatementUnavailableException : BaseFhirException
{
    public override IEnumerable<BaseFhirHttpError> Errors { get; } =
        [new ProxyServerError()];

    public override string Message => ProxyServerError.Message;
}
