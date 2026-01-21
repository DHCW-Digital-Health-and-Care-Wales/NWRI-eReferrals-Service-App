using FluentValidation.Results;
using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public class BundleValidationException : BaseFhirException
{
    private readonly IEnumerable<ValidationFailure> _validationErrors;

    public BundleValidationException(IEnumerable<ValidationFailure> validationErrors)
    {
        _validationErrors = validationErrors;
    }

    public override IEnumerable<BaseFhirHttpError> Errors => _validationErrors.Select(error => new InvalidBundleError(error.ErrorMessage));
    public override string Message => $"Bundle validation failure: {string.Join(';', _validationErrors.Select(x => x.ErrorMessage))}.";
}


