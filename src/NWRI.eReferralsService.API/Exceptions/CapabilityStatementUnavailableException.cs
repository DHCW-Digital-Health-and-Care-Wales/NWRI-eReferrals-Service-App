using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public class CapabilityStatementUnavailableException : BaseFhirException
{
    public CapabilityStatementUnavailableException(BaseFhirHttpError error)
    {
        Errors = [error];
    }

    public override IEnumerable<BaseFhirHttpError> Errors { get; }
    public override string Message => Errors.First().DiagnosticsMessage;
}
