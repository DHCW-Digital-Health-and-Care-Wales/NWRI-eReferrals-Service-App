using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public sealed class CapabilityStatementUnavailableException : BaseFhirException
{
    public CapabilityStatementUnavailableException(string resourcePath, string cause)
    {
        Errors = [new CapabilityStatementError(resourcePath, cause)];
    }

    public override IEnumerable<BaseFhirHttpError> Errors { get; }
    public override string Message => Errors.First().DiagnosticsMessage;
}
