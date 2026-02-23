using FluentValidation.Results;

namespace NWRI.eReferralsService.API.Exceptions;

public class WpasSchemaValidationException : BundleValidationException
{
    public WpasSchemaValidationException(string validationDetails)
        : base([new ValidationFailure("", "WPAS payload JSON schema validation failed.")])
    {
        ValidationDetails = validationDetails;
    }

    public string ValidationDetails { get; }

    public override string Message => $"WPAS payload JSON schema validation failed. Details: {ValidationDetails}";
}