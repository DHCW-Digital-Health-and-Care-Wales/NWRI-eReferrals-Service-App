using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.API.Exceptions;
using WCCG.eReferralsService.Unit.Tests.Extensions;
using FooObject = (string strValue, int intVal);

namespace WCCG.eReferralsService.Unit.Tests.Exceptions;

public class NotSuccessfulApiCallExceptionTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForValidationErrors()
    {
        //Arrange
        var statusCode = _fixture.Create<HttpStatusCode>();
        var errorMessages = _fixture.CreateMany<string>().ToList();
        var errorMessagesJson = JsonSerializer.Serialize(errorMessages);

        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, new Dictionary<string, object?> { { "validationErrors", errorMessagesJson } })
            .Create();

        var errors = errorMessages.Select(e => new NotSuccessfulApiResponseError(FhirHttpErrorCodes.ReceiverBadRequest, e));
        var expectedMessage = $"API cal returned: {(int)statusCode}. {string.Join(';', errors.Select(x => x.DiagnosticsMessage))}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(FhirHttpErrorCodes.ReceiverBadRequest));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverBadRequest)]
    [InlineData(HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests, FhirHttpErrorCodes.TooManyRequests)]
    [InlineData(HttpStatusCode.NotFound, FhirHttpErrorCodes.ReceiverNotFound)]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForGeneralExtension(HttpStatusCode statusCode, string errorCode)
    {
        //Arrange
        var extensionDictionary = new Dictionary<string, object>
        {
            { _fixture.Create<string>(), _fixture.Create<FooObject>() },
            { _fixture.Create<string>(), _fixture.Create<FooObject>() },
            { _fixture.Create<string>(), _fixture.Create<FooObject>() }
        };

        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, extensionDictionary!)
            .Create();

        var errorParts = extensionDictionary.Select(pair => $"{pair.Key}: {JsonSerializer.Serialize(pair.Value)}");
        var error = new NotSuccessfulApiResponseError(FhirHttpErrorCodes.ReceiverBadRequest, string.Join(";", errorParts));
        var expectedMessage = $"API cal returned: {(int)statusCode}. {error.DiagnosticsMessage}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(errorCode));
    }

    [Fact]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForUnexpectedError()
    {
        //Arrange
        var statusCode = _fixture.Create<HttpStatusCode>();

        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, new Dictionary<string, object?>())
            .With(x => x.Detail, (string?)null)
            .Create();

        var error = new NotSuccessfulApiResponseError(FhirHttpErrorCodes.ReceiverBadRequest, "Unexpected error");
        var expectedMessage = $"API cal returned: {(int)statusCode}. {error.DiagnosticsMessage}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(FhirHttpErrorCodes.ReceiverUnavailable));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverBadRequest)]
    [InlineData(HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests, FhirHttpErrorCodes.TooManyRequests)]
    [InlineData(HttpStatusCode.NotFound, FhirHttpErrorCodes.ReceiverNotFound)]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForRegularError(HttpStatusCode statusCode, string errorCode)
    {
        //Arrange
        var errorMessage = _fixture.Create<string>();

        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, new Dictionary<string, object?>())
            .With(x => x.Detail, errorMessage)
            .Create();

        var expectedMessage = $"API cal returned: {(int)statusCode}. Receiver error. {errorMessage}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(errorCode));
    }

    [Fact]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForRawContent()
    {
        //Arrange
        var statusCode = _fixture.Create<HttpStatusCode>();
        var rawContent = _fixture.Create<string>();

        var expectedMessage = $"API cal returned: {(int)statusCode}. Raw content: {rawContent}";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, rawContent);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<UnexpectedError>());
    }
}
