using FluentValidation.Results;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;

namespace WCCG.eReferralsService.API.Exceptions;

public class HeaderValidationException : BaseFhirException
{
    private readonly Dictionary<ValidationErrorCode, Func<string, BaseHeaderError>> _errorTypeDictionary = new()
    {
        { ValidationErrorCode.MissingRequiredHeaderCode, message => new MissingRequiredHeaderError(message) },
        { ValidationErrorCode.InvalidHeaderCode, message => new InvalidHeaderError(message) }
    };

    private readonly IEnumerable<ValidationFailure> _validationErrors;

    public HeaderValidationException(IEnumerable<ValidationFailure> validationErrors)
    {
        _validationErrors = validationErrors;
    }

    public override IEnumerable<BaseFhirHttpError> Errors =>
        _validationErrors.Select(error =>
        {
            if (!Enum.TryParse<ValidationErrorCode>(error.ErrorCode, out var errorCode))
            {
                throw new NotSupportedException($"Wrong error code: {error.ErrorCode}.");
            }

            return _errorTypeDictionary[errorCode](error.ErrorMessage);
        });

    public override string Message => $"Header(s) validation failure: {string.Join(';', _validationErrors.Select(x => x.ErrorMessage))}";
}
