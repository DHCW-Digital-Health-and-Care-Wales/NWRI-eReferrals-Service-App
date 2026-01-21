using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public class FhirProfileValidationException : BaseFhirException
{
    private readonly IEnumerable<string> _validationErrors;

    public FhirProfileValidationException(IEnumerable<string> validationErrors)
    {
        _validationErrors = validationErrors;
    }

    public override IEnumerable<BaseFhirHttpError> Errors => _validationErrors.Select(error => new InvalidBundleError(error));
    public override string Message => $"FHIR profile validation failure: {string.Join(';', _validationErrors.Select(x => x))}.";
}


