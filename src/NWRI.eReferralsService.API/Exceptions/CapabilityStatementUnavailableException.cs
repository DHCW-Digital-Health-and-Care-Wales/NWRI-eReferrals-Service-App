using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public sealed class CapabilityStatementUnavailableException : BaseFhirException
{
    public override IEnumerable<BaseFhirHttpError> Errors { get; } =
        [
            new ProxyServerError("CapabilityStatement resource is unavailable.")
        ];

    public override string Message => "CapabilityStatement resource is unavailable.";
}
