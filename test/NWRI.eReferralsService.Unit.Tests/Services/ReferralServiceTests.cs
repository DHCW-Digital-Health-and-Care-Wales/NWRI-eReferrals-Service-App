using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Mappers;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Models.WPAS.Responses;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.API.Validators;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using NWRI.eReferralsService.Unit.Tests.TestFixtures;
using Task = System.Threading.Tasks.Task;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class ReferralServiceTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();

    private static readonly Lazy<WpasJsonSchemaValidator> SharedSchemaValidator = new(() =>
    {
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment
            .SetupGet(x => x.ContentRootPath)
            .Returns(Path.Combine(GetRepoRootPath(), "test", "NWRI.eReferralsService.Unit.Tests", "TestData"));

        return new WpasJsonSchemaValidator(hostEnvironment.Object);
    });

    public ReferralServiceTests()
    {
        _fixture.Register(() => new Bundle
        {
            Id = _fixture.Create<string>(),
            Type = Bundle.BundleType.Message
        });

        _fixture.Register<IHeaderDictionary>(() => new HeaderDictionary { { _fixture.Create<string>(), _fixture.Create<string>() } });

        _fixture.Mock<IFhirBundleProfileValidator>()
            .Setup(x => x.ValidateAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfileValidationOutput
            {
                IsSuccessful = true,
                Errors = new List<string>()
            });

        _fixture.Mock<IRequestFhirHeadersDecoder>()
            .Setup(x => x.GetDecodedSourceSystem(It.IsAny<string?>()))
            .Returns(_fixture.Create<string>());

        _fixture.Mock<IRequestFhirHeadersDecoder>()
            .Setup(x => x.GetDecodedUserRole(It.IsAny<string?>()))
            .Returns(_fixture.Create<string>());

        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<WpasCreateReferralResponse>());

        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CancelReferralAsync(It.IsAny<WpasCancelReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<WpasCancelReferralResponse>());

        _fixture.Register(() => new WpasCreateReferralRequestMapper());

        _fixture.Mock<IHostEnvironment>()
            .SetupGet(x => x.ContentRootPath)
            .Returns(Path.Combine(GetRepoRootPath(), "test", "NWRI.eReferralsService.Unit.Tests", "TestData"));

        _fixture.Register(() => SharedSchemaValidator.Value);
    }

    private static string GetRepoRootPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NWRI.eReferralsService.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate repository root (NWRI.eReferralsService.sln) from test execution directory.");
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenWpasSchemaValidationFails()
    {
        // Arrange
        var bundle = CreateMessageBundle(FhirConstants.BarsMessageReasonNew);
        var receiverOrganisation = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Organization>()
            .First(o => string.Equals(o.Name, "Receiving/performing Organization", StringComparison.Ordinal));
        receiverOrganisation.Identifier.First().Value = "TP2V"; // invalid length: schema requires exactly 5

        var bundleJson = JsonSerializer.Serialize(bundle, _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCreateReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCreateReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var sut = CreateReferralService();

        // Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        var exception = await action.Should().ThrowAsync<WpasSchemaValidationException>();
        exception.Which.ValidationDetails.Should().Contain("InstanceLocation");
        _fixture.Mock<IWpasApiClient>().Verify(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _fixture.Mock<IEventLogger>().Verify(x => x.Audit(It.Is<IAuditEvent>(e => e is EventCatalogue.MapFhirToWpas)), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenWpasMappingThrowsException()
    {
        // Arrange
        var bundle = CreateMessageBundle(FhirConstants.BarsMessageReasonNew);
        var patient = bundle.Entry
            .Select(e => e.Resource)
            .OfType<Patient>()
            .First();
        patient.Identifier.Clear();
        var bundleJson = JsonSerializer.Serialize(bundle, _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCreateReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCreateReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var sut = CreateReferralService();

        // Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<BundleValidationException>();
        _fixture.Mock<IWpasApiClient>().Verify(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _fixture.Mock<IEventLogger>().Verify(x => x.LogError(It.Is<IErrorEvent>(e => e is EventCatalogue.MapFhirToWpasFailed), It.IsAny<Exception>()), Times.Once);
        _fixture.Mock<IEventLogger>().Verify(x => x.Audit(It.Is<IAuditEvent>(e => e is EventCatalogue.MapFhirToWpas)), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldRouteToCreateWhenReasonIsNew()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew), _jsonSerializerOptions);
        var expectedResponse = _fixture.Create<WpasCreateReferralResponse>();
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCreateReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCreateReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var sut = CreateReferralService();

        //Act
        var result = await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        //Assert
        result.Should().BeEmpty();
        _fixture.Mock<IWpasApiClient>().Verify(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldIncludeWpasReferralIdInAuditEventWhenPresentInResponse()
    {
        // Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew), _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCreateReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCreateReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var expectedReferralId = WpasCreateReferralRequestBuilder.ValidReferralId;
        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WpasCreateReferralResponse
            {
                ReferralId = expectedReferralId,
                System = _fixture.Create<string>(),
                AssigningAuthority = _fixture.Create<string>(),
                OrganisationCode = _fixture.Create<string>(),
                OrganisationName = _fixture.Create<string>(),
                ReferralCreationTimestamp = _fixture.Create<string>()
            });

        var sut = CreateReferralService();

        // Act
        await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        _fixture.Mock<IEventLogger>().Verify(
            x => x.Audit(It.Is<EventCatalogue.AuditReferralAccepted>(e => e.WpasReferralId == expectedReferralId)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenReasonUnsupported()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle("not-supported"), _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var sut = CreateReferralService();

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        //Assert
        await action.Should().ThrowAsync<BundleValidationException>();
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenReasonIsNewAndStatusIsNotActive()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew, RequestStatus.Revoked), _jsonSerializerOptions);
        var headers = _fixture.Create<IHeaderDictionary>();

        var sut = CreateReferralService();

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        //Assert
        await action.Should().ThrowAsync<BundleValidationException>();
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

        var sut = CreateReferralService();

        //Act
        await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

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

        var sut = CreateReferralService();

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

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

        var expectedModel = BundleCreateReferralModel.FromBundle(bundle);

        var modelArgs = new List<BundleCreateReferralModel>();
        _fixture.Mock<IValidator<BundleCreateReferralModel>>().Setup(x => x.ValidateAsync(Capture.In(modelArgs), It.IsAny<CancellationToken>()));

        var sut = CreateReferralService();

        //Act
        await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        //Assert
        modelArgs[0].Should().BeEquivalentTo(expectedModel);
        _fixture.Mock<IValidator<BundleCreateReferralModel>>().Verify(x => x.ValidateAsync(It.IsAny<BundleCreateReferralModel>(), It.IsAny<CancellationToken>()));
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

        _fixture.Mock<IValidator<BundleCreateReferralModel>>().Setup(x => x.ValidateAsync(It.IsAny<BundleCreateReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var sut = CreateReferralService();

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

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

        _fixture.Mock<IValidator<BundleCreateReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCreateReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var failureOutput = new ProfileValidationOutput
        {
            IsSuccessful = false,
            Errors = new List<string> { "Profile validation failed" }
        };

        _fixture.Mock<IFhirBundleProfileValidator>()
            .Setup(x => x.ValidateAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureOutput);

        var sut = CreateReferralService();

        //Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        //Assert
        await action.Should().ThrowAsync<FhirProfileValidationException>();
    }

    [Fact]
    public async Task CreateReferralAsyncShouldReturnOutputBundleJson()
    {
        //Arrange
        var bundleJson = JsonSerializer.Serialize(CreateMessageBundle(FhirConstants.BarsMessageReasonNew), _jsonSerializerOptions);
        var expectedResponse = _fixture.Create<WpasCreateReferralResponse>();
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var sut = CreateReferralService();

        //Act
        var result = await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        //Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldRouteToCancelWhenReasonIsUpdateAndStatusIsRevoked()
    {
        // Arrange
        var bundleJson = JsonSerializer.Serialize(
            CreateMessageBundle(FhirConstants.BarsMessageReasonUpdate, RequestStatus.Revoked),
            _jsonSerializerOptions);

        var expectedResponse = _fixture.Create<WpasCancelReferralResponse>();
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCancelReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCancelReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CancelReferralAsync(It.IsAny<WpasCancelReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var sut = CreateReferralService();

        // Act
        var result = await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _fixture.Mock<IWpasApiClient>().Verify(x => x.CancelReferralAsync(It.IsAny<WpasCancelReferralRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldRouteToCancelWhenReasonIsUpdateAndStatusIsEnteredInError()
    {
        // Arrange
        var bundleJson = JsonSerializer.Serialize(
            CreateMessageBundle(FhirConstants.BarsMessageReasonUpdate, RequestStatus.EnteredInError),
            _jsonSerializerOptions);

        var expectedResponse = _fixture.Create<WpasCancelReferralResponse>();
        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCancelReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCancelReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CancelReferralAsync(It.IsAny<WpasCancelReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var sut = CreateReferralService();

        // Act
        var result = await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelReferralShouldThrowWhenFhirProfileValidationFailed()
    {
        // Arrange
        var bundleJson = JsonSerializer.Serialize(
            CreateMessageBundle(FhirConstants.BarsMessageReasonUpdate, RequestStatus.Revoked),
            _jsonSerializerOptions);

        var headers = _fixture.Create<IHeaderDictionary>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCancelReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCancelReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var failureOutput = new ProfileValidationOutput
        {
            IsSuccessful = false,
            Errors = new List<string> { "Profile validation failed" }
        };

        _fixture.Mock<IFhirBundleProfileValidator>()
            .Setup(x => x.ValidateAsync(It.IsAny<Bundle>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureOutput);

        var sut = CreateReferralService();

        // Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<FhirProfileValidationException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task CancelReferralShouldThrowWhenWpasReturnsNonSuccess(HttpStatusCode statusCode)
    {
        // Arrange
        var bundleJson = JsonSerializer.Serialize(
            CreateMessageBundle(FhirConstants.BarsMessageReasonUpdate, RequestStatus.Revoked),
            _jsonSerializerOptions);

        var headers = _fixture.Create<IHeaderDictionary>();
        var problemDetails = _fixture.Create<ProblemDetails>();

        _fixture.Mock<IValidator<HeadersModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<HeadersModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IValidator<BundleCancelReferralModel>>()
            .Setup(x => x.ValidateAsync(It.IsAny<BundleCancelReferralModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CancelReferralAsync(It.IsAny<WpasCancelReferralRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSuccessfulApiCallException(statusCode, problemDetails));

        var sut = CreateReferralService();

        // Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        (await action.Should().ThrowAsync<NotSuccessfulApiCallException>())
            .Which.StatusCode.Should().Be(statusCode);
    }

    private ReferralService CreateReferralService()
    {
        var eventLogger = _fixture.Mock<IEventLogger>().Object;
        var wpasApiClient = _fixture.Mock<IWpasApiClient>().Object;
        var fhirBundleProfileValidator = _fixture.Mock<IFhirBundleProfileValidator>().Object;
        var headerValidator = _fixture.Mock<IValidator<HeadersModel>>().Object;
        var jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();

        var wpasCreateReferralRequestMapper = _fixture.Create<WpasCreateReferralRequestMapper>();
        var wpasJsonSchemaValidator = SharedSchemaValidator.Value;

        var serviceProvider = _fixture.Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IValidator<BundleCreateReferralModel>)))
            .Returns(_fixture.Mock<IValidator<BundleCreateReferralModel>>().Object);
        serviceProvider
            .Setup(x => x.GetService(typeof(IValidator<BundleCancelReferralModel>)))
            .Returns(_fixture.Mock<IValidator<BundleCancelReferralModel>>().Object);

        var referralValidationService = new ReferralBundleValidationService(
            fhirBundleProfileValidator,
            eventLogger,
            serviceProvider.Object
        );

        var referralWorkflowProcessor = new ReferralWorkflowProcessor(
            referralValidationService,
            wpasCreateReferralRequestMapper,
            wpasJsonSchemaValidator,
            jsonSerializerOptions,
            wpasApiClient,
            eventLogger
        );

        return new ReferralService(
            headerValidator,
            jsonSerializerOptions,
            eventLogger,
            _fixture.Mock<IRequestFhirHeadersDecoder>().Object,
            referralWorkflowProcessor
        );
    }

    private static Bundle CreateMessageBundle(string reasonCode, RequestStatus? serviceRequestStatus = RequestStatus.Active)
    {
        const string serviceRequestCategorySystem = "https://fhir.nhs.uk/CodeSystem/message-category-servicerequest";
        const string barsUseCaseCategorySystem = "https://fhir.nhs.uk/CodeSystem/usecases-categories-bars";
        const string nhsNumberSystem = "https://fhir.nhs.uk/Id/nhs-number";
        const string nhsNumberVerificationStatusSystem =
            "https://fhir.hl7.org.uk/CodeSystem/UKCore-NHSNumberVerificationStatusEngland";

        const string serviceRequestId = "sr-1";
        var messageHeader = new MessageHeader
        {
            Reason = new CodeableConcept(FhirConstants.BarsMessageReasonSystem, reasonCode),
            Event = new Coding("https://example.org/fhir/message-events", "ereferral"),
            Source = new MessageHeader.MessageSourceComponent
            {
                Endpoint = "https://unit-tests/source"
            },
            Focus = [new ResourceReference($"ServiceRequest/{serviceRequestId}")]
        };

        var serviceRequest = new ServiceRequest
        {
            Id = serviceRequestId,
            IntentElement = new Code<RequestIntent>(RequestIntent.Order),
            Subject = new ResourceReference("Patient/pat-1"),
            AuthoredOn = "2024-08-20",
            Category =
            [
                new CodeableConcept
                {
                    Coding =
                    [
                        new Coding(serviceRequestCategorySystem, "6"),
                        new Coding(barsUseCaseCategorySystem, "01")
                    ]
                }
            ]
        };

        if (serviceRequestStatus is not null)
        {
            serviceRequest.StatusElement = new Code<RequestStatus>(serviceRequestStatus.Value);
        }

        var encounter = new Encounter
        {
            Id = "enc-1",
            Status = Encounter.EncounterStatus.Finished,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB"),
            Identifier =
            [
                new Identifier
                {
                    Value = "record-id"
                }
            ]
        };

        var patient = new Patient
        {
            Id = "pat-1",
            Name =
            [
                new HumanName
                {
                    Family = "Jones",
                    Given = ["Julie"]
                }
            ],
            Address =
            [
                new Address
                {
                    Line = ["22 Brightside Crescent"],
                    City = "Overtown",
                    PostalCode = "LS10 4YU"
                }
            ],
            BirthDate = "1959-05-04",
            Gender = AdministrativeGender.Female,
            Identifier =
            [
                new Identifier
                {
                    System = nhsNumberSystem,
                    Value = "3478526985",
                    Extension =
                    [
                        new Extension
                        {
                            Url = "https://example.org/fhir/StructureDefinition/nhs-number-verification",
                            Value = new CodeableConcept
                            {
                                Coding =
                                [
                                    new Coding(nhsNumberVerificationStatusSystem, "01")
                                ]
                            }
                        }
                    ]
                }
            ]
        };

        var receiverOrganisation = new Organization
        {
            Name = "Receiving/performing Organization",
            Identifier =
            [
                new Identifier
                {
                    Value = "TP2VC"
                }
            ]
        };

        var senderOrganisation = new Organization
        {
            Name = "Sender Organization",
            Identifier =
            [
                new Identifier
                {
                    Value = "15"
                }
            ]
        };

        var practitioner = new Practitioner
        {
            Identifier =
            [
                new Identifier
                {
                    Value = "01-99999"
                }
            ]
        };

        var condition = new Condition
        {
            Subject = new ResourceReference("Patient/pat-1"),
            Code = new CodeableConcept
            {
                Coding =
                [
                    new Coding
                    {
                        Display = "ReasonForReferral"
                    }
                ]
            }
        };

        return new Bundle
        {
            Type = Bundle.BundleType.Message,
            Entry =
            [
                new Bundle.EntryComponent
                {
                    Resource = messageHeader
                },
                new Bundle.EntryComponent
                {
                    Resource = serviceRequest
                },
                new Bundle.EntryComponent
                {
                    Resource = encounter
                },
                new Bundle.EntryComponent
                {
                    Resource = patient
                },
                new Bundle.EntryComponent
                {
                    Resource = receiverOrganisation
                },
                new Bundle.EntryComponent
                {
                    Resource = senderOrganisation
                },
                new Bundle.EntryComponent
                {
                    Resource = practitioner
                },
                new Bundle.EntryComponent
                {
                    Resource = condition
                }
            ]
        };
    }
}
