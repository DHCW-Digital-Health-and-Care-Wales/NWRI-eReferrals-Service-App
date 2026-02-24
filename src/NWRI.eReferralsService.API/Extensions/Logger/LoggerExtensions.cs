using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Hl7.Fhir.Serialization;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.API.Extensions.Logger;

[ExcludeFromCodeCoverage]
public static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Called {methodName}.")]
    public static partial void CalledMethod(this ILogger logger, string methodName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Header(s) validation error.")]
    public static partial void HeadersValidationError(this ILogger logger, HeaderValidationException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Bundle validation error.")]
    public static partial void BundleValidationError(this ILogger logger, BundleValidationException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "FHIR profile validation error.")]
    public static partial void FhirProfileValidationError(this ILogger logger, FhirProfileValidationException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Request parameter validation error.")]
    public static partial void RequestParameterValidationError(this ILogger logger, RequestParameterValidationException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to deserialize bundle.")]
    public static partial void BundleDeserializationFailure(this ILogger logger, DeserializationFailedException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid JSON.")]
    public static partial void InvalidJson(this ILogger logger, JsonException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while calling the WPAS API.")]
    public static partial void ApiCallError(this ILogger logger, HttpRequestException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "WPAS API returned not successful response.")]
    public static partial void NotSuccessfulApiResponseError(this ILogger logger, NotSuccessfulApiCallException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unexpected error.")]
    public static partial void UnexpectedError(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Proxy endpoint not implemented (BaRS Core Spec).")]
    public static partial void ProxyNotImplemented(this ILogger logger, ProxyNotImplementedException exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to decode base64 string.")]
    public static partial void Base64DecodingFailure(this ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "CapabilityStatement JSON resource not found.")]
    public static partial void CapabilityStatementJsonNotFound(this ILogger logger, CapabilityStatementUnavailableException exception);
}
