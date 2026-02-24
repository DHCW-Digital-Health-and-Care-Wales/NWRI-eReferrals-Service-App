using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public class CapabilityStatementUnavailableException : BaseFhirException
{
    private const string ErrorMessage = "CapabilityStatement resource is unavailable.";

    public CapabilityStatementUnavailableException(Exception cause, string resourcePath)
    {
        Cause = cause;

        Errors =
        [
            new ProxyServerError(BuildDiagnostics(cause, resourcePath))
        ];
    }

    public Exception Cause { get; }

    public override IEnumerable<BaseFhirHttpError> Errors { get; }

    public override string Message => ErrorMessage;

    private static string BuildDiagnostics(Exception cause, string resourcePath)
    {
        return cause switch
        {
            FileNotFoundException =>
                $"CapabilityStatement JSON resource was not found. ResourcePath='{resourcePath}'.",

            _ =>
                $"CapabilityStatement JSON resource could not be loaded. ResourcePath='{resourcePath}'."
        };
    }
}
