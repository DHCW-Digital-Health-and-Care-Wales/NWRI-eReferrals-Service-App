using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using RichardSzalay.MockHttp;
using WCCG.eReferralsService.API.Configuration;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.API.Exceptions;
using WCCG.eReferralsService.API.Extensions;
using WCCG.eReferralsService.API.Models;
using WCCG.eReferralsService.API.Services;
using WCCG.eReferralsService.API.Validators;
using WCCG.eReferralsService.Unit.Tests.Extensions;
using Task = System.Threading.Tasks.Task;

namespace WCCG.eReferralsService.Unit.Tests.Services;

public class ReferralServiceTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly PasReferralsApiConfig _pasReferralsApiConfig;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();

    public ReferralServiceTests()
    {
        _pasReferralsApiConfig = _fixture.Build<PasReferralsApiConfig>()
            .With(x => x.GetReferralEndpoint, _fixture.Create<string>() + "/{0}")
            .Create();
        _fixture.Mock<IOptions<PasReferralsApiConfig>>().SetupGet(x => x.Value).Returns(_pasReferralsApiConfig);

        _fixture.Register(() => new Bundle
        {
            Id = _fixture.Create<string>(),
            Type = Bundle.BundleType.Message
        });

        _fixture.Register<IHeaderDictionary>(() => new HeaderDictionary { { _fixture.Create<string>(), _fixture.Create<string>() } });

        _fixture.Mock<IFhirBundleProfileValidator>()
            .Setup(x => x.Validate(It.IsAny<Bundle>()))
            .Returns(new ProfileValidationOutput
            {
                IsSuccessful = true,
                Errors = new List<string>()
            });
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldRouteToCreateWhenReasonIsNew()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew), _jsonSerializerOptions);
        var expectedResponse = _fixture.Create<string>();
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_pasReferralsApiConfig.CreateReferralEndpoint}")
            .WithContent(bundleJson)
            .WithHeaders(HeaderNames.ContentType, FhirConstants.FhirMediaType)
            .Respond(FhirConstants.FhirMediaType, expectedResponse);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var result = await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenReasonIsDeleteUntilCancelImplemented()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonDelete), _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var sut = CreateReferralService(new MockHttpMessageHandler().ToHttpClient());

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        await action.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenReasonUnsupported()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle("update"), _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var sut = CreateReferralService(new MockHttpMessageHandler().ToHttpClient());

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        await action.Should().ThrowAsync<RequestParameterValidationException>();
    }

    [Fact]
    public async Task CreateReferralAsyncShouldValidateHeaders()
    {
        //Arrange
        var bundle = CreateMessageBundle(FhirConstants.BarsMessageReasonNew);
        var bundleJson = JsonSerializer.Serialize(bundle, _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var expectedModel = HeadersModel.FromHeaderDictionary(headers);

        var modelArgs = new List<HeadersModel>();
        _fixture.Mock<IValidator<HeadersModel>>().Setup(x => x.ValidateAsync(Capture.In(modelArgs), It.IsAny<CancellationToken>()));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_pasReferralsApiConfig.CreateReferralEndpoint}")
            .Respond(FhirConstants.FhirMediaType, _fixture.Create<string>());

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        modelArgs[0].Should().BeEquivalentTo(expectedModel);
        _fixture.Mock<IValidator<HeadersModel>>().Verify(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateReferralAsyncShouldThrowWhenInvalidHeaders()
    {
        //Arrange
        var bundle = CreateMessageBundle(FhirConstants.BarsMessageReasonNew);
        var bundleJson = JsonSerializer.Serialize(bundle, _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var validationFailures = _fixture.CreateMany<ValidationFailure>().ToList();
        var validationResult = _fixture.Build<ValidationResult>()
            .With(x => x.Errors, validationFailures)
            .Create();

        _fixture.Mock<IValidator<HeadersModel>>().Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_pasReferralsApiConfig.CreateReferralEndpoint}")
            .Respond(FhirConstants.FhirMediaType, _fixture.Create<string>());

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        (await action.Should().ThrowAsync<HeaderValidationException>())
            .Which.Message.Should().Contain(string.Join(';', validationFailures.Select(x => x.ErrorMessage)));
    }

    [Fact]
    public async Task CreateReferralAsyncShouldValidateBundle()
    {
        //Arrange
        var bundle = CreateMessageBundle(FhirConstants.BarsMessageReasonNew);
        var bundleJson = JsonSerializer.Serialize(bundle, _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var expectedModel = BundleModel.FromBundle(bundle);

        var modelArgs = new List<BundleModel>();
        _fixture.Mock<IValidator<BundleModel>>().Setup(x => x.ValidateAsync(Capture.In(modelArgs), It.IsAny<CancellationToken>()));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_pasReferralsApiConfig.CreateReferralEndpoint}")
            .Respond(FhirConstants.FhirMediaType, _fixture.Create<string>());

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        modelArgs[0].Should().BeEquivalentTo(expectedModel);
        _fixture.Mock<IValidator<BundleModel>>().Verify(x => x.ValidateAsync(It.IsAny<BundleModel>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task CreateReferralAsyncShouldThrowWhenValidationFailed()
    {
        //Arrange
        var bundle = CreateMessageBundle(FhirConstants.BarsMessageReasonNew);
        var bundleJson = JsonSerializer.Serialize(bundle, _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var validationFailures = _fixture.CreateMany<ValidationFailure>().ToList();
        var validationResult = _fixture.Build<ValidationResult>()
            .With(x => x.Errors, validationFailures)
            .Create();

        _fixture.Mock<IValidator<BundleModel>>().Setup(x => x.ValidateAsync(It.IsAny<BundleModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var sut = CreateReferralService(new MockHttpMessageHandler().ToHttpClient());

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        (await action.Should().ThrowAsync<BundleValidationException>())
            .Which.Message.Should().Contain(string.Join(';', validationFailures.Select(x => x.ErrorMessage)));
    }

    [Fact]
    public async Task CreateReferralAsyncShouldThrowWhenFhirProfileValidationFailed()
    {
        //Arrange
        var bundle = CreateMessageBundle(FhirConstants.BarsMessageReasonNew);
        var bundleJson = JsonSerializer.Serialize(bundle, _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var failureOutput = new ProfileValidationOutput
        {
            IsSuccessful = false,
            Errors = new List<string> { "Profile validation failed" }
        };

        _fixture.Mock<IFhirBundleProfileValidator>()
            .Setup(x => x.Validate(It.IsAny<Bundle>()))
            .Returns(failureOutput);

        var sut = CreateReferralService(new MockHttpMessageHandler().ToHttpClient());

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        await action.Should().ThrowAsync<FhirProfileValidationException>();
    }

    [Fact]
    public async Task CreateReferralAsyncShouldReturnOutputBundleJson()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew), _jsonSerializerOptions);
        var expectedResponse = _fixture.Create<string>();
        var headers = _fixture.Create<IHeaderDictionary>();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_pasReferralsApiConfig.CreateReferralEndpoint}")
            .WithContent(bundleJson)
            .WithHeaders(HeaderNames.ContentType, FhirConstants.FhirMediaType)
            .Respond(FhirConstants.FhirMediaType, expectedResponse);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var result = await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        result.Should().Be(expectedResponse);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task CreateReferralAsyncShouldThrowWhenNot200ResponseWithProblemDetails(HttpStatusCode statusCode)
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew), _jsonSerializerOptions);
        var problemDetails = _fixture.Create<ProblemDetails>();

        var headers = _fixture.Create<IHeaderDictionary>();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_pasReferralsApiConfig.CreateReferralEndpoint}")
            .Respond(statusCode, JsonContent.Create(problemDetails));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        var exception = (await action.Should().ThrowAsync<NotSuccessfulApiCallException>()).Subject.ToList();
        exception[0].StatusCode.Should().Be(statusCode);
        exception[0].Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task CreateReferralAsyncShouldThrowWhenNotJsonAndNot200Response(HttpStatusCode statusCode)
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew), _jsonSerializerOptions);
        var stringContent = _fixture.Create<string>();

        var headers = _fixture.Create<IHeaderDictionary>();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_pasReferralsApiConfig.CreateReferralEndpoint}")
            .Respond(statusCode, new StringContent(stringContent));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson);

        //Assert
        var exception = (await action.Should().ThrowAsync<NotSuccessfulApiCallException>()).Subject.ToList();
        exception[0].StatusCode.Should().Be(statusCode);
        exception[0].Errors.Should().AllSatisfy(e => e.Should().BeOfType<UnexpectedError>());
    }

    [Theory]
    [InlineData("123")]
    [InlineData(null)]
    public async Task GetReferralAsyncShouldThrowWhenInvalidGuid(string? invalidGuid)
    {
        //Arrange
        var headers = _fixture.Create<IHeaderDictionary>();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get,
                string.Format(CultureInfo.InvariantCulture, $"/{_pasReferralsApiConfig.GetReferralEndpoint}", invalidGuid))
            .Respond(FhirConstants.FhirMediaType, _fixture.Create<string>());

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var action = async () => await sut.GetReferralAsync(headers, invalidGuid);

        //Assert
        await action.Should().ThrowAsync<RequestParameterValidationException>()
            .WithMessage("Request parameter validation failure. Parameter name: id, Error: Id should be a valid GUID.");
    }

    [Fact]
    public async Task GetReferralAsyncShouldValidateHeaders()
    {
        //Arrange
        var id = Guid.NewGuid().ToString();
        var headers = _fixture.Create<IHeaderDictionary>();

        var expectedModel = HeadersModel.FromHeaderDictionary(headers);

        var modelArgs = new List<HeadersModel>();
        _fixture.Mock<IValidator<HeadersModel>>().Setup(x => x.ValidateAsync(Capture.In(modelArgs), It.IsAny<CancellationToken>()));

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, $"/{_pasReferralsApiConfig.GetReferralEndpoint}", id))
            .Respond(FhirConstants.FhirMediaType, _fixture.Create<string>());

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        await sut.GetReferralAsync(headers, id);

        //Assert
        modelArgs[0].Should().BeEquivalentTo(expectedModel);
        _fixture.Mock<IValidator<HeadersModel>>().Verify(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetReferralAsyncShouldThrowWhenInvalidHeaders()
    {
        //Arrange
        var id = Guid.NewGuid().ToString();
        var headers = _fixture.Create<IHeaderDictionary>();

        var validationFailures = _fixture.CreateMany<ValidationFailure>().ToList();
        var validationResult = _fixture.Build<ValidationResult>()
            .With(x => x.Errors, validationFailures)
            .Create();

        _fixture.Mock<IValidator<HeadersModel>>().Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, $"/{_pasReferralsApiConfig.GetReferralEndpoint}", id))
            .Respond(FhirConstants.FhirMediaType, _fixture.Create<string>());

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var action = async () => await sut.GetReferralAsync(headers, id);

        //Assert
        (await action.Should().ThrowAsync<HeaderValidationException>())
            .Which.Message.Should().Contain(string.Join(';', validationFailures.Select(x => x.ErrorMessage)));
    }

    [Fact]
    public async Task GetReferralAsyncShouldReturnOutputBundleJson()
    {
        //Arrange
        var id = Guid.NewGuid().ToString();

        var expectedResponse = _fixture.Create<string>();
        var headers = _fixture.Create<IHeaderDictionary>();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, $"/{_pasReferralsApiConfig.GetReferralEndpoint}", id))
            .Respond(FhirConstants.FhirMediaType, expectedResponse);

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var result = await sut.GetReferralAsync(headers, id);

        //Assert
        result.Should().Be(expectedResponse);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task GetReferralAsyncShouldThrowWhenNot200Response(HttpStatusCode statusCode)
    {
        //Arrange
        var id = Guid.NewGuid().ToString();
        var problemDetails = _fixture.Create<ProblemDetails>();

        var headers = _fixture.Create<IHeaderDictionary>();

        var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Get, string.Format(CultureInfo.InvariantCulture, $"/{_pasReferralsApiConfig.GetReferralEndpoint}", id))
            .Respond(statusCode, JsonContent.Create(problemDetails));

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://some.com");

        var sut = CreateReferralService(httpClient);

        //Act
        var action = async () => await sut.GetReferralAsync(headers, id);

        //Assert
        (await action.Should().ThrowAsync<NotSuccessfulApiCallException>())
            .Which.StatusCode.Should().Be(statusCode);
    }

    private ReferralService CreateReferralService(HttpClient httpClient)
    {
        return new ReferralService(
            httpClient,
            _fixture.Mock<IOptions<PasReferralsApiConfig>>().Object,
            _fixture.Mock<IValidator<BundleModel>>().Object,
            _fixture.Mock<IFhirBundleProfileValidator>().Object,
            _fixture.Mock<IValidator<HeadersModel>>().Object,
            _jsonSerializerOptions
        );
    }

    private static Bundle CreateMessageBundle(string reasonCode)
    {
        var messageHeader = new MessageHeader
        {
            Reason = new CodeableConcept(FhirConstants.BarsMessageReasonSystem, reasonCode)
        };

        return new Bundle
        {
            Type = Bundle.BundleType.Message,
            Entry =
            [
                new Bundle.EntryComponent
                {
                    Resource = messageHeader
                }
            ]
        };
    }
}
