using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using FooObject = (string strValue, int intVal);

namespace NWRI.eReferralsService.Unit.Tests.Exceptions;

public class NotSuccessfulApiCallExceptionTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverServerError)]
    [InlineData(HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForValidationErrors(HttpStatusCode statusCode, string errorCode)
    {
        //Arrange
        var errorMessages = _fixture.CreateMany<string>().ToList();
        var errorMessagesJson = JsonSerializer.Serialize(errorMessages);

        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, new Dictionary<string, object?> { { "validationErrors", errorMessagesJson } })
            .Create();

        var errors = errorMessages.Select(e => new NotSuccessfulApiResponseError(errorCode, e));
        var expectedMessage = $"API call returned: {(int)statusCode}. {string.Join(';', errors.Select(x => x.DiagnosticsMessage))}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(errorCode));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverServerError)]
    [InlineData(HttpStatusCode.TooManyRequests, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    [InlineData(HttpStatusCode.NotFound, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForGeneralExtension(HttpStatusCode statusCode, string errorCode)
    {
        //Arrange
        var extensionDictionary = new Dictionary<string, object?>
        {
            { _fixture.Create<string>(), _fixture.Create<FooObject>() },
            { _fixture.Create<string>(), _fixture.Create<FooObject>() },
            { _fixture.Create<string>(), _fixture.Create<FooObject>() }
        };

        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, extensionDictionary)
            .Create();

        var errorParts = extensionDictionary.Select(pair => $"{pair.Key}: {JsonSerializer.Serialize(pair.Value)}");
        var error = new NotSuccessfulApiResponseError(FhirHttpErrorCodes.ReceiverBadRequest, string.Join(";", errorParts));
        var expectedMessage = $"API call returned: {(int)statusCode}. {error.DiagnosticsMessage}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(errorCode));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverServerError)]
    [InlineData(HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    [InlineData(HttpStatusCode.NotFound, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    [InlineData(HttpStatusCode.Created, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForUnexpectedError(HttpStatusCode statusCode, string errorCode)
    {
        //Arrange
        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, new Dictionary<string, object?>())
            .With(x => x.Detail, (string?)null)
            .Create();

        var error = new NotSuccessfulApiResponseError(FhirHttpErrorCodes.ReceiverBadRequest, "Unexpected error");
        var expectedMessage = $"API call returned: {(int)statusCode}. {error.DiagnosticsMessage}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(errorCode));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverServerError)]
    [InlineData(HttpStatusCode.TooManyRequests, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    [InlineData(HttpStatusCode.NotFound, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForRegularError(HttpStatusCode statusCode, string errorCode)
    {
        //Arrange
        var errorMessage = _fixture.Create<string>();

        var problemDetails = _fixture.Build<ProblemDetails>()
            .With(x => x.Extensions, new Dictionary<string, object?>())
            .With(x => x.Detail, errorMessage)
            .Create();

        var expectedMessage = $"API call returned: {(int)statusCode}. Receiver error. {errorMessage}.";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, problemDetails);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>()
            .Which.Code.Should().Be(errorCode));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, FhirHttpErrorCodes.ReceiverServerError)]
    [InlineData(HttpStatusCode.BadRequest, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    [InlineData(HttpStatusCode.NotFound, FhirHttpErrorCodes.ReceiverUnprocessableEntity)]
    public void ShouldCorrectlyCreateNotSuccessfulApiCallExceptionForRawContent(HttpStatusCode statusCode, string errorCode)
    {
        //Arrange
        var rawContent = _fixture.Create<string>();

        var expectedMessage = $"API call returned: {(int)statusCode}. Raw content: {rawContent}";

        //Act
        var exception = new NotSuccessfulApiCallException(statusCode, rawContent);

        //Assert
        exception.StatusCode.Should().Be(statusCode);
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e =>
        {
            e.Should().BeOfType<NotSuccessfulApiResponseError>();
            e.Code.Should().Be(errorCode);
            e.DiagnosticsMessage.Should().Contain(rawContent);
        });
    }
}
