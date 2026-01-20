using System.Text;
using System.Text.Json;
using AutoFixture;
using FluentValidation;
using FluentValidation.TestHelper;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Extensions;
using WCCG.eReferralsService.API.Models;
using WCCG.eReferralsService.API.Validators;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Validators;

public class HeadersModelValidatorTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    private readonly HeadersModelValidator _sut;

    public HeadersModelValidatorTests()
    {
        _sut = _fixture.CreateWithFrozen<HeadersModelValidator>();
        _sut.ClassLevelCascadeMode = CascadeMode.Continue;
        _sut.RuleLevelCascadeMode = CascadeMode.Stop;

        _fixture.Register(() => new Identifier(_fixture.Create<string>(), _fixture.Create<string>()));
        _fixture.Register(() => new Organization { Id = _fixture.Create<string>() });
        _fixture.Register(() => new Device { Id = _fixture.Create<string>() });
        _fixture.Register(() => new PractitionerRole { Id = _fixture.Create<string>() });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldContainErrorWhenTargetIdentifierEmpty(string? value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.TargetIdentifier, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.TargetIdentifier)
            .WithErrorMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.TargetIdentifier))
            .WithErrorCode(ValidationErrorCode.MissingRequiredHeaderCode.ToString());
    }

    [Fact]
    public void ShouldContainErrorWhenTargetIdentifierNotValid()
    {
        //Arrange
        var model = _fixture.Create<HeadersModel>();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.TargetIdentifier)
            .WithErrorMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.TargetIdentifier, nameof(Identifier)))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Fact]
    public void ShouldNotContainErrorWhenTargetIdentifierValid()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.TargetIdentifier, CreateValidBase64<Identifier>())
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.TargetIdentifier);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldContainErrorWhenEndUserOrganisationEmpty(string? value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.EndUserOrganisation, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.EndUserOrganisation)
            .WithErrorMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.EndUserOrganisation))
            .WithErrorCode(ValidationErrorCode.MissingRequiredHeaderCode.ToString());
    }

    [Fact]
    public void ShouldContainErrorWhenEndUserOrganisationNotValid()
    {
        //Arrange
        var model = _fixture.Create<HeadersModel>();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.EndUserOrganisation)
            .WithErrorMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.EndUserOrganisation, nameof(Organization)))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Fact]
    public void ShouldNotContainErrorWhenEndUserOrganisationValid()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.EndUserOrganisation, CreateValidBase64<Organization>())
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.EndUserOrganisation);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldContainErrorWhenRequestingSoftwareEmpty(string? value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.RequestingSoftware, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.RequestingSoftware)
            .WithErrorMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.RequestingSoftware))
            .WithErrorCode(ValidationErrorCode.MissingRequiredHeaderCode.ToString());
    }

    [Fact]
    public void ShouldContainErrorWhenRequestingSoftwareNotValid()
    {
        //Arrange
        var model = _fixture.Create<HeadersModel>();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.RequestingSoftware)
            .WithErrorMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.RequestingSoftware, nameof(Device)))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Fact]
    public void ShouldNotContainErrorWhenRequestingSoftwareValid()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.RequestingSoftware, CreateValidBase64<Device>())
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.RequestingSoftware);
    }

    [Fact]
    public void ShouldContainErrorWhenRequestingPractitionerPresentAndNotValid()
    {
        //Arrange
        var model = _fixture.Create<HeadersModel>();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.RequestingPractitioner)
            .WithErrorMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.RequestingPractitioner, nameof(PractitionerRole)))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Fact]
    public void ShouldNotContainErrorWhenRequestingPractitionerNotPresent()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .Without(x => x.RequestingPractitioner)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.RequestingPractitioner);
    }

    [Fact]
    public void ShouldNotContainErrorWhenRequestingPractitionerPresentAndValid()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.RequestingPractitioner, CreateValidBase64<PractitionerRole>())
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.RequestingPractitioner);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldContainErrorWhenRequestIdEmpty(string? value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.RequestId, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.RequestId)
            .WithErrorMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.RequestId))
            .WithErrorCode(ValidationErrorCode.MissingRequiredHeaderCode.ToString());
    }

    [Fact]
    public void ShouldContainErrorWhenRequestIdNotValid()
    {
        //Arrange
        var model = _fixture.Create<HeadersModel>();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.RequestId)
            .WithErrorMessage(ValidationMessages.NotGuidFormat(RequestHeaderKeys.RequestId))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Fact]
    public void ShouldNotContainErrorWhenRequestIdValid()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.RequestId, Guid.NewGuid().ToString)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.RequestId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldContainErrorWhenCorrelationIdEmpty(string? value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.CorrelationId, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.CorrelationId)
            .WithErrorMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.CorrelationId))
            .WithErrorCode(ValidationErrorCode.MissingRequiredHeaderCode.ToString());
    }

    [Fact]
    public void ShouldContainErrorWhenCorrelationIdNotValid()
    {
        //Arrange
        var model = _fixture.Create<HeadersModel>();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.CorrelationId)
            .WithErrorMessage(ValidationMessages.NotGuidFormat(RequestHeaderKeys.CorrelationId))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Fact]
    public void ShouldNotContainErrorWhenCorrelationIdValid()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.CorrelationId, Guid.NewGuid().ToString)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldContainErrorWhenUseContextEmpty(string? value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.UseContext, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.UseContext)
            .WithErrorMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.UseContext))
            .WithErrorCode(ValidationErrorCode.MissingRequiredHeaderCode.ToString());
    }

    [Fact]
    public void ShouldContainErrorWhenUseContextNotValid()
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.UseContext, "|")
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.UseContext)
            .WithErrorMessage(ValidationMessages.NotExpectedFormat(RequestHeaderKeys.UseContext,
                RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.UseContext)))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Theory]
    [InlineData("a4t2|servicerequest-response")]
    [InlineData("validation|a4t2")]
    [InlineData("new|servicerequest-response|validation")]
    [InlineData("a4t2|servicerequest-response|validation|new")]
    public void ShouldNotContainErrorWhenUseContextValid(string useContext)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.UseContext, useContext)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.UseContext);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldContainErrorWhenAcceptEmpty(string? value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.Accept, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.Accept)
            .WithErrorMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.Accept))
            .WithErrorCode(ValidationErrorCode.MissingRequiredHeaderCode.ToString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abc;123")]
    [InlineData("application/fhir+json;123")]
    [InlineData("application/fhir+json;version=a.3")]
    public void ShouldContainErrorWhenAcceptNotValid(string accept)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.Accept, accept)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldHaveValidationErrorFor(x => x.Accept)
            .WithErrorMessage(ValidationMessages.NotExpectedFormat(RequestHeaderKeys.Accept,
                RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept)))
            .WithErrorCode(ValidationErrorCode.InvalidHeaderCode.ToString());
    }

    [Theory]
    [InlineData("application/fhir+json; version=1.2.0")]
    [InlineData("application/fhir+json;version=1.2.0")]
    [InlineData("version=1.2.0;    application/fhir+json")]
    public void ShouldNotContainErrorWhenAcceptValid(string value)
    {
        //Arrange
        var model = _fixture.Build<HeadersModel>()
            .With(x => x.Accept, value)
            .Create();

        //Act
        var validationResult = _sut.TestValidate(model);

        //Assert
        validationResult.ShouldNotHaveValidationErrorFor(x => x.Accept);
    }

    private string CreateValidBase64<T>()
    {
        var obj = _fixture.Create<T>();
        var json = JsonSerializer.Serialize(obj, _jsonSerializerOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}
