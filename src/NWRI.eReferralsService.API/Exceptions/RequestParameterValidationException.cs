using WCCG.eReferralsService.API.Errors;

namespace WCCG.eReferralsService.API.Exceptions;

public class RequestParameterValidationException : BaseFhirException
{
    private readonly string _parameterName;
    private readonly string _validationErrorMessage;

    public RequestParameterValidationException(string parameterName, string validationErrorMessage)
    {
        _parameterName = parameterName;
        _validationErrorMessage = validationErrorMessage;

        Errors = [new InvalidRequestParameterError(_parameterName, _validationErrorMessage)];
    }

    public override IEnumerable<BaseFhirHttpError> Errors { get; }

    public override string Message =>
        $"Request parameter validation failure. Parameter name: {_parameterName}, Error: {_validationErrorMessage}.";
}
