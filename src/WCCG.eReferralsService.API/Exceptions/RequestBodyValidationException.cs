using WCCG.eReferralsService.API.Errors;

namespace WCCG.eReferralsService.API.Exceptions;

public class RequestBodyValidationException : BaseFhirException
{
    private readonly string _details;

    public RequestBodyValidationException(string details)
    {
        _details = details;
        Errors = [new BundleDeserializationError(details)];
    }

    public override IEnumerable<BaseFhirHttpError> Errors { get; }

    public override string Message => $"Request body validation failure. {_details}";
}
