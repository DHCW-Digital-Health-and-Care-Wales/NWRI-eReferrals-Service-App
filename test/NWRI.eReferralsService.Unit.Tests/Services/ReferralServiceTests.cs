using System.Net;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using Json.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.API.Validators;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using Task = System.Threading.Tasks.Task;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class ReferralServiceTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();

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

        _fixture.Mock<IWpasOutpatientReferralMapper>()
            .Setup(x => x.Map(It.IsAny<BundleCreateReferralModel>()))
            .Returns(new WpasCreateReferralRequest
            {
                RecordId = "record-id",
                ContractDetails = new WpasCreateReferralRequest.ContractDetailsModel
                {
                    ProviderOrganisationCode = "TP2VC"
                },
                PatientDetails = new WpasCreateReferralRequest.PatientDetailsModel
                {
                    NhsNumber = "3478526985",
                    NhsNumberStatusIndicator = "01",
                    PatientName = new WpasCreateReferralRequest.PatientDetailsModel.PatientNameModel
                    {
                        Surname = "Jones",
                        FirstName = "Julie"
                    },
                    BirthDate = "19590504",
                    Sex = "F",
                    UsualAddress = new WpasCreateReferralRequest.PatientDetailsModel.UsualAddressModel
                    {
                        NoAndStreet = "22 Brightside Crescent",
                        Town = "Overtown",
                        Postcode = "LS10 4YU",
                        Locality = ""
                    }
                },
                ReferralDetails = new WpasCreateReferralRequest.ReferralDetailsModel
                {
                    OutpatientReferralSource = "15",
                    ReferringOrganisationCode = "TP2VC",
                    ServiceTypeRequested = "6",
                    ReferrerCode = "01-99999",
                    AdministrativeCategory = "01",
                    DateOfReferral = "20240820",
                    MainSpecialty = "130",
                    ReferrerPriorityType = "2",
                    ReasonForReferral = "Reason",
                    ReferralIdentifier = "referral-id"
                }
            });

        _fixture.Mock<IJsonSchemaValidator>()
            .Setup(x => x.Validate(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<string>()))
            .Returns(CreateValidSchemaResult());
    }

    private static EvaluationResults CreateValidSchemaResult()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        using var doc = JsonDocument.Parse("{}");
        return schema.Evaluate(doc.RootElement);
    }

    private static EvaluationResults CreateInvalidSchemaResult()
    {
        var schema = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        using var doc = JsonDocument.Parse("{}");
        return schema.Evaluate(doc.RootElement);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenWpasSchemaValidationFails()
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

        _fixture.Mock<IJsonSchemaValidator>()
            .Setup(x => x.Validate(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<string>()))
            .Returns(CreateInvalidSchemaResult());

        var sut = CreateReferralService();

        // Act
        var action = async () => await sut.ProcessMessageAsync(headers, bundleJson, CancellationToken.None);

        // Assert
        await action.Should().ThrowAsync<BundleValidationException>();
        _fixture.Mock<IWpasApiClient>().Verify(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyErrorEventWasLogged(_fixture.Mock<ILogger<ReferralService>>(), "WpasSchemaValidationFailed");
        _fixture.Mock<IEventLogger>().Verify(x => x.Audit(It.Is<IAuditEvent>(e => e is EventCatalogue.MapFhirToWpas)), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsyncShouldThrowWhenWpasMappingThrowsException()
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

        _fixture.Mock<IWpasOutpatientReferralMapper>()
            .Setup(x => x.Map(It.IsAny<BundleCreateReferralModel>()))
            .Throws(new InvalidOperationException("boom"));

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

        var expectedReferralId = "140:12345678";
        _fixture.Mock<IWpasApiClient>()
            .Setup(x => x.CreateReferralAsync(It.IsAny<WpasCreateReferralRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WpasCreateReferralResponse
            {
                ReferralId = expectedReferralId
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
        return new ReferralService(
            _fixture.Mock<IWpasApiClient>().Object,
            _fixture.Mock<IValidator<BundleCreateReferralModel>>().Object,
            _fixture.Mock<IValidator<BundleCancelReferralModel>>().Object,
            _fixture.Mock<IFhirBundleProfileValidator>().Object,
            _fixture.Mock<IValidator<HeadersModel>>().Object,
            new JsonSerializerOptions().ForFhirExtended(),
            _fixture.Mock<IEventLogger>().Object,
            _fixture.Mock<IRequestFhirHeadersDecoder>().Object
            ,
            _fixture.Mock<IWpasOutpatientReferralMapper>().Object,
            _fixture.Mock<IJsonSchemaValidator>().Object,
            _fixture.Mock<ILogger<ReferralService>>().Object
        );
    }

    private static void VerifyErrorEventWasLogged<T>(Mock<ILogger<T>> logger, string eventName)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.Is<EventId>(e => string.Equals(e.Name, eventName, StringComparison.Ordinal)),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static Bundle CreateMessageBundle(string reasonCode, RequestStatus? serviceRequestStatus = RequestStatus.Active)
    {
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
            Subject = new ResourceReference("Patient/pat-1")
        };

        if (serviceRequestStatus is not null)
        {
            serviceRequest.StatusElement = new Code<RequestStatus>(serviceRequestStatus.Value);
        }

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
                }
            ]
        };
    }
}
