using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;

namespace NWRI.eReferralsService.API.Exceptions;

public class NotSuccessfulApiCallException : BaseFhirException
{
    private const string ValidationErrorsKey = "validationErrors";
    private string ExceptionMessage { get; }

    public HttpStatusCode StatusCode { get; init; }
    public override IEnumerable<BaseFhirHttpError> Errors { get; }
    public override string Message => ExceptionMessage;

    public NotSuccessfulApiCallException(HttpStatusCode statusCode, ProblemDetails problemDetails)
    {
        StatusCode = statusCode;

        var errors = GetErrors(problemDetails).ToList();
        Errors = errors;

        ExceptionMessage = $"API call returned: {(int)statusCode}. {string.Join(';', errors.Select(x => x.DiagnosticsMessage))}.";
    }

    public NotSuccessfulApiCallException(HttpStatusCode statusCode, string rawContent)
    {
        StatusCode = statusCode;

        var wpasMessage = string.IsNullOrWhiteSpace(rawContent)
            ? "WPAS API call failed."
            : rawContent;

        Errors =
        [
            new NotSuccessfulApiResponseError(
                GetFhirErrorCode(StatusCode),
                wpasMessage)
        ];

        ExceptionMessage = $"API call returned: {(int)statusCode}. Raw content: {rawContent}";
    }

    private IEnumerable<BaseFhirHttpError> GetErrors(ProblemDetails problemDetails)
    {
        if (problemDetails.Extensions.TryGetValue(ValidationErrorsKey, out var validationErrors) && validationErrors is not null)
        {
            var validationErrorsJson = validationErrors.ToString();
            if (validationErrorsJson != null)
            {
                var errorList = JsonSerializer.Deserialize<List<string>>(validationErrorsJson);
                if (errorList != null)
                {
                    return errorList.Select(e => new NotSuccessfulApiResponseError(GetFhirErrorCode(StatusCode), e));
                }
            }
        }

        if (problemDetails.Extensions.Count > 0)
        {
            var errorParts = problemDetails.Extensions.Select(pair => $"{pair.Key}: {JsonSerializer.Serialize(pair.Value)}");
            return
            [
                new NotSuccessfulApiResponseError(
                    GetFhirErrorCode(StatusCode),
                    string.Join(";", errorParts))
            ];
        }

        if (problemDetails.Detail is null)
        {
            return
            [
                new NotSuccessfulApiResponseError(
                    GetFhirErrorCode(StatusCode),
                    "Unexpected error")
            ];
        }

        return
        [
            new NotSuccessfulApiResponseError(
                GetFhirErrorCode(StatusCode), problemDetails.Detail)
        ];
    }

    private static string GetFhirErrorCode(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.InternalServerError
            ? FhirHttpErrorCodes.ReceiverServerError
            : FhirHttpErrorCodes.ReceiverUnprocessableEntity;
    }
}
