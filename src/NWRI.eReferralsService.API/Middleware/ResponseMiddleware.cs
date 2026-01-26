using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Helpers;
using Polly.Timeout;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.API.Middleware;

public class ResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<ResponseMiddleware> _logger;
    private readonly IEventLogger _eventLogger;

    public ResponseMiddleware(
        RequestDelegate next,
        JsonSerializerOptions serializerOptions,
        ILogger<ResponseMiddleware> logger,
        IEventLogger eventLogger)
    {
        _next = next;
        _serializerOptions = serializerOptions;
        _logger = logger;
        _eventLogger = eventLogger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        AddResponseHeaders(context);
        AddOperationIdHeader(context);

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.BadRequest;
        OperationOutcome body;

        var path = context.Request.Path.Value ?? string.Empty;

        switch (exception)
        {
            case HeaderValidationException headerValidationException:
                _logger.HeadersValidationError(headerValidationException);

                _eventLogger.LogError(new EventCatalogue.ErrValFhirViolation(path), headerValidationException);

                body = OperationOutcomeCreator.CreateOperationOutcome(headerValidationException);
                break;

            case BundleValidationException bundleValidationException:
                _logger.BundleValidationError(bundleValidationException);

                _eventLogger.LogError(new EventCatalogue.ErrValFhirViolation(path), bundleValidationException);

                body = OperationOutcomeCreator.CreateOperationOutcome(bundleValidationException);
                break;

            case FhirProfileValidationException fhirProfileValidationException:
                _logger.FhirProfileValidationError(fhirProfileValidationException);

                _eventLogger.LogError(new EventCatalogue.ErrValFhirViolation(path), fhirProfileValidationException);

                body = OperationOutcomeCreator.CreateOperationOutcome(fhirProfileValidationException);
                break;

            case DeserializationFailedException deserializationFailedException:
                _logger.BundleDeserializationFailure(deserializationFailedException);

                _eventLogger.LogError(new EventCatalogue.ErrValMalformedJson(path), deserializationFailedException);

                body = OperationOutcomeCreator.CreateOperationOutcome(
                    new BundleDeserializationError(deserializationFailedException.Message));
                break;

            case JsonException jsonException:
                _logger.InvalidJson(jsonException);

                _eventLogger.LogError(new EventCatalogue.ErrValMalformedJson(path), jsonException);

                body = OperationOutcomeCreator.CreateOperationOutcome(new BundleDeserializationError(jsonException.Message));
                break;

            case NotSuccessfulApiCallException notSuccessfulApiCallException:
                _logger.NotSuccessfulApiResponseError(notSuccessfulApiCallException);

                statusCode = notSuccessfulApiCallException.StatusCode == HttpStatusCode.InternalServerError
                    ? HttpStatusCode.ServiceUnavailable
                    : notSuccessfulApiCallException.StatusCode;

                _eventLogger.LogError(
                    statusCode == HttpStatusCode.GatewayTimeout
                        ? new EventCatalogue.ErrIntWpasTimeout(path)
                        : new EventCatalogue.ErrIntWpasConnectionFail(path),
                    notSuccessfulApiCallException);
                body = OperationOutcomeCreator.CreateOperationOutcome(notSuccessfulApiCallException);
                break;

            case RequestParameterValidationException requestParameterValidationException:
                _logger.RequestParameterValidationError(requestParameterValidationException);

                _eventLogger.LogError(new EventCatalogue.ErrValFhirViolation(path), requestParameterValidationException);

                body = OperationOutcomeCreator.CreateOperationOutcome(requestParameterValidationException);
                break;

            case HttpRequestException requestException:
                _logger.ApiCallError(requestException);

                _eventLogger.LogError(new EventCatalogue.ErrIntWpasConnectionFail(path), requestException);

                statusCode = HttpStatusCode.ServiceUnavailable;
                body = OperationOutcomeCreator.CreateOperationOutcome(new ApiCallError(requestException.Message));
                break;

            case TimeoutRejectedException timeoutRejectedException:
                _logger.ApiCallError(new HttpRequestException(timeoutRejectedException.Message, timeoutRejectedException));

                statusCode = HttpStatusCode.GatewayTimeout;
                _eventLogger.LogError(new EventCatalogue.ErrIntWpasTimeout(path), timeoutRejectedException);
                body = OperationOutcomeCreator.CreateOperationOutcome(new ApiCallError(timeoutRejectedException.Message));
                break;

            default:
                _logger.UnexpectedError(exception);

                _eventLogger.LogError(new EventCatalogue.ErrInternalHandler(path), exception);

                statusCode = HttpStatusCode.InternalServerError;
                body = OperationOutcomeCreator.CreateOperationOutcome(new UnexpectedError(exception.Message));
                break;
        }

        context.Response.ContentType = FhirConstants.FhirMediaType;
        context.Response.StatusCode = (int)statusCode;
        await context.Response.Body.WriteAsync(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body, _serializerOptions)));
    }

    private static void AddResponseHeaders(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(RequestHeaderKeys.RequestId, out var requestId))
        {
            context.Response.Headers.TryAdd(RequestHeaderKeys.RequestId, requestId);
        }

        if (context.Request.Headers.TryGetValue(RequestHeaderKeys.CorrelationId, out var correlationId))
        {
            context.Response.Headers.TryAdd(RequestHeaderKeys.CorrelationId, correlationId);
        }
    }

    private static void AddOperationIdHeader(HttpContext context)
    {
        context.Response.Headers.TryAdd("X-Operation-Id", Activity.Current?.TraceId.ToString());
    }
}
