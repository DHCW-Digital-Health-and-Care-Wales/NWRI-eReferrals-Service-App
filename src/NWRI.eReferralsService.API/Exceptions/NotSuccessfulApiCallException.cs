using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;

namespace WCCG.eReferralsService.API.Exceptions;

public class NotSuccessfulApiCallException : BaseFhirException
{
    private const string ValidationErrorsKey = "validationErrors";
    private string ExceptionMessage { get; }

    private readonly Dictionary<HttpStatusCode, string> _fhirErrorCodeDictionary = new()
    {
        { HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverBadRequest },
        { HttpStatusCode.TooManyRequests, FhirHttpErrorCodes.TooManyRequests },
        { HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverUnavailable },
        { HttpStatusCode.NotFound, FhirHttpErrorCodes.ReceiverNotFound }
    };

    public HttpStatusCode StatusCode { get; init; }
    public override IEnumerable<BaseFhirHttpError> Errors { get; }
    public override string Message => ExceptionMessage;

    public NotSuccessfulApiCallException(HttpStatusCode statusCode, ProblemDetails problemDetails)
    {
        StatusCode = statusCode;

        var errors = GetErrors(problemDetails).ToList();
        Errors = errors;

        ExceptionMessage = $"API cal returned: {(int)statusCode}. {string.Join(';', errors.Select(x => x.DiagnosticsMessage))}.";
    }

    public NotSuccessfulApiCallException(HttpStatusCode statusCode, string rawContent)
    {
        StatusCode = statusCode;
        Errors = [new UnexpectedError("PAS API call failed.")];
        ExceptionMessage = $"API cal returned: {(int)statusCode}. Raw content: {rawContent}";
    }

    private IEnumerable<BaseFhirHttpError> GetErrors(ProblemDetails problemDetails)
    {
        if (problemDetails.Extensions.TryGetValue(ValidationErrorsKey, out var errorMessages))
        {
            var errorList = JsonSerializer.Deserialize<List<string>>(errorMessages!.ToString()!);
            return errorList!.Select(e => new NotSuccessfulApiResponseError(FhirHttpErrorCodes.ReceiverBadRequest, e));
        }

        if (problemDetails.Extensions.Count > 0)
        {
            var errorParts = problemDetails.Extensions.Select(pair => $"{pair.Key}: {JsonSerializer.Serialize(pair.Value)}");
            return
            [
                new NotSuccessfulApiResponseError(
                    _fhirErrorCodeDictionary.GetValueOrDefault(StatusCode, FhirHttpErrorCodes.ReceiverUnavailable),
                    string.Join(";", errorParts))
            ];
        }

        if (problemDetails.Detail is null)
        {
            return [new NotSuccessfulApiResponseError(FhirHttpErrorCodes.ReceiverUnavailable, "Unexpected error")];
        }

        return
        [
            new NotSuccessfulApiResponseError(
                _fhirErrorCodeDictionary.GetValueOrDefault(StatusCode, FhirHttpErrorCodes.ReceiverUnavailable), problemDetails.Detail)
        ];
    }
}
