using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Exceptions;
using WCCG.eReferralsService.API.Extensions;
using WCCG.eReferralsService.API.Middleware;
using WCCG.eReferralsService.Unit.Tests.Extensions;
using Task = System.Threading.Tasks.Task;

namespace WCCG.eReferralsService.Integration.Tests.Middleware;

public class ResponseMiddlewareTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public async Task ShouldHandleHeaderValidationException()
    {
        //Arrange
        var validationFailures = _fixture.Build<ValidationFailure>()
            .With(x => x.ErrorCode, ValidationErrorCode.MissingRequiredHeaderCode.ToString)
            .CreateMany().ToList();
        var exception = new HeaderValidationException(validationFailures);

        var errorMessages = validationFailures.Select(x => x.ErrorMessage);
        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component =>
        {
            component.Code.Should().Be(OperationOutcome.IssueType.Required);
            component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
            errorMessages.Should().Contain(component.Diagnostics);
        });
    }

    [Fact]
    public async Task ShouldHandleBundleValidationException()
    {
        //Arrange
        var validationFailures = _fixture.Build<ValidationFailure>()
            .With(x => x.ErrorCode, ValidationErrorCode.MissingRequiredHeaderCode.ToString)
            .CreateMany().ToList();
        var exception = new BundleValidationException(validationFailures);

        var errorMessages = validationFailures.Select(x => x.ErrorMessage);
        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component =>
        {
            component.Code.Should().Be(OperationOutcome.IssueType.Invalid);
            component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
            errorMessages.Should().Contain(component.Diagnostics);
        });
    }

    [Fact]
    public async Task ShouldHandleDeserializationFailedException()
    {
        //Arrange
        var exception = _fixture.Create<DeserializationFailedException>();

        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component =>
        {
            component.Code.Should().Be(OperationOutcome.IssueType.Structure);
            component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
            component.Diagnostics.Should().Contain(exception.Message);
        });
    }

    [Fact]
    public async Task ShouldHandleJsonException()
    {
        //Arrange
        var exception = _fixture.Create<JsonException>();

        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component =>
        {
            component.Code.Should().Be(OperationOutcome.IssueType.Structure);
            component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
            component.Diagnostics.Should().Contain(exception.Message);
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.BadRequest, HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound, HttpStatusCode.NotFound)]
    public async Task ShouldHandleNotSuccessfulApiCallException(HttpStatusCode inCode, HttpStatusCode outCode)
    {
        //Arrange
        var exception = new NotSuccessfulApiCallException(inCode, _fixture.Create<ProblemDetails>());

        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(outCode);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component => { component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error); });
    }

    [Fact]
    public async Task ShouldHandleRequestParameterValidationException()
    {
        //Arrange
        var parameterName = _fixture.Create<string>();
        var errorMessage = _fixture.Create<string>();
        var exception = new RequestParameterValidationException(parameterName, errorMessage);

        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component =>
        {
            component.Code.Should().Be(OperationOutcome.IssueType.Invalid);
            component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
            component.Diagnostics.Should().Contain(parameterName).And.Contain(errorMessage);
        });
    }

    [Fact]
    public async Task ShouldHandleHttpRequestException()
    {
        //Arrange
        var exception = _fixture.Create<HttpRequestException>();

        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component =>
        {
            component.Code.Should().Be(OperationOutcome.IssueType.Transient);
            component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
            component.Diagnostics.Should().Contain(exception.Message);
        });
    }

    [Fact]
    public async Task ShouldHandleDefaultException()
    {
        //Arrange
        var exception = _fixture.Create<Exception>();
        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        var operationOutcome = JsonSerializer.Deserialize<OperationOutcome>(await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions().ForFhirExtended())!;
        operationOutcome.Issue.Should().AllSatisfy(component =>
        {
            component.Code.Should().Be(OperationOutcome.IssueType.Transient);
            component.Severity.Should().Be(OperationOutcome.IssueSeverity.Error);
            component.Diagnostics.Should().Contain(exception.Message);
        });
    }

    [Fact]
    public async Task ShouldTryToAddHeadersWhenException()
    {
        //Arrange
        var exception = _fixture.Create<Exception>();
        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHostWithException(exception);

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.Headers.GetValues("X-Operation-Id").Should()
            .NotBeNullOrEmpty();

        response.Content.Headers.GetValues(HeaderNames.ContentType).Should()
            .NotBeNull()
            .And.Contain(FhirConstants.FhirMediaType);

        response.Headers.GetValues(RequestHeaderKeys.RequestId).Should()
            .NotBeNull()
            .And.Contain(requestId);

        response.Headers.GetValues(RequestHeaderKeys.CorrelationId).Should()
            .NotBeNull()
            .And.Contain(correlationId);
    }

    [Fact]
    public async Task ShouldTryToAddHeadersWhenSuccessful()
    {
        //Arrange
        var requestId = _fixture.Create<string>();
        var correlationId = _fixture.Create<string>();

        var host = StartHost();

        //Act
        var response = await host.GetTestServer()
            .CreateRequest(HostProvider.TestEndpoint)
            .AddHeader(RequestHeaderKeys.RequestId, requestId)
            .AddHeader(RequestHeaderKeys.CorrelationId, correlationId)
            .GetAsync();

        //Assert
        response.Headers.GetValues("X-Operation-Id").Should()
            .NotBeNullOrEmpty();

        response.Headers.GetValues(RequestHeaderKeys.RequestId).Should()
            .NotBeNull()
            .And.Contain(requestId);

        response.Headers.GetValues(RequestHeaderKeys.CorrelationId).Should()
            .NotBeNull()
            .And.Contain(correlationId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static IHost StartHostWithException(Exception exception)
    {
        return HostProvider.StartHostWithEndpoint(_ => throw exception,
            addServices: services => services.AddSingleton(new JsonSerializerOptions().ForFhirExtended()),
            configureApp: app => app.UseMiddleware<ResponseMiddleware>());
    }

    private static IHost StartHost()
    {
        return HostProvider.StartHostWithEndpoint(_ => Task.CompletedTask,
            addServices: services => services.AddSingleton(new JsonSerializerOptions().ForFhirExtended()),
            configureApp: app => app.UseMiddleware<ResponseMiddleware>());
    }
}
