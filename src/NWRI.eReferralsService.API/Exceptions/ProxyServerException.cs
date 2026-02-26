using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public sealed class ProxyServerException : BaseFhirException
{
    private readonly string _message;

    public ProxyServerException(string message)
    {
        _message = message;
        Errors = [new ProxyServerError(message)];
    }

    public override IEnumerable<BaseFhirHttpError> Errors { get; }

    public override string Message => _message;
}