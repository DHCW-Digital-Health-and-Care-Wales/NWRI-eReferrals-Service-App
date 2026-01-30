using System.Net;
using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public class ProxyNotImplementedException : BaseFhirException
{
    public override IEnumerable<BaseFhirHttpError> Errors { get; }
    public HttpStatusCode StatusCode { get; }
    public override string Message { get; }

    public ProxyNotImplementedException(string message)
    {
        StatusCode = HttpStatusCode.NotImplemented;
        Message = message;
        Errors = [new ProxyNotImplementedError(message)];
    }
}
