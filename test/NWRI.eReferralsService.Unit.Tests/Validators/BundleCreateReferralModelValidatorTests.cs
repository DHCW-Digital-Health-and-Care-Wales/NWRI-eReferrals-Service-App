using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Validators;
using NWRI.eReferralsService.Unit.Tests.Extensions;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Possible null reference argument.

namespace NWRI.eReferralsService.Unit.Tests.Validators;

public class BundleCreateReferralModelValidatorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    private readonly BundleCreateReferralModelValidator _sut;

    public BundleCreateReferralModelValidatorTests()
    {
        _sut = _fixture.CreateWithFrozen<BundleCreateReferralModelValidator>();
        _sut.ClassLevelCascadeMode = CascadeMode.Continue;
    }

    private static BundleCreateReferralModel CreateValidModelFromExampleBundle()
    {
        var bundleJson = File.ReadAllText("TestData/example-bundle.json");

        var options = new JsonSerializerOptions()
            .ForFhir(ModelInfo.ModelInspector);

        var bundle = JsonSerializer.Deserialize<Bundle>(bundleJson, options);

        return BundleCreateReferralModel.FromBundle(bundle);
    }

    [Fact]
    public void ExampleBundleShouldBeValid()
    {
        var model = CreateValidModelFromExampleBundle();

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldContainErrorWhenMessageHeaderNull()
    {
        var model = CreateValidModelFromExampleBundle();
        model.MessageHeader = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.MessageHeader)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(MessageHeader)));
    }

    [Fact]
    public void ShouldContainErrorWhenServiceRequestNull()
    {
        var model = CreateValidModelFromExampleBundle();
        model.ServiceRequest = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ServiceRequest)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(ServiceRequest)));
    }

    [Fact]
    public void ShouldContainErrorWhenPatientNull()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Patient = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Patient)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(Patient)));
    }

    [Fact]
    public void ShouldContainErrorWhenEncounterNull()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Encounter = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Encounter)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(Encounter)));
    }

    [Fact]
    public void ShouldContainErrorWhenCarePlanNull()
    {
        var model = CreateValidModelFromExampleBundle();
        model.CarePlan = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.CarePlan)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(CarePlan)));
    }

    [Fact]
    public void ShouldContainErrorWhenHealthcareServiceNull()
    {
        var model = CreateValidModelFromExampleBundle();
        model.HealthcareService = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.HealthcareService)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(HealthcareService)));
    }

    [Fact]
    public void ShouldContainErrorWhenPatientNhsNumberMissing()
    {
        var model = CreateValidModelFromExampleBundle();

        // Remove NHS number identifier
        model.Patient.Identifier = model.Patient.Identifier
            .Where(i => !string.Equals(i.System, "https://fhir.nhs.uk/Id/nhs-number", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var result = _sut.TestValidate(model);

        result.Errors.Should().Contain(e => e.ErrorMessage == "Patient.Identifier is required");
    }

    [Fact]
    public void ShouldContainErrorWhenServiceRequestBasedOnMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.ServiceRequest.BasedOn = [];

        var result = _sut.TestValidate(model);

        result.Errors.Should().Contain(e => e.ErrorMessage == "ServiceRequest.BasedOn is required");
    }

    [Fact]
    public void ShouldContainErrorWhenServiceRequestOccurrencePeriodMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.ServiceRequest.Occurrence = null;

        var result = _sut.TestValidate(model);

        result.Errors.Should().Contain(e => e.ErrorMessage == "ServiceRequest.occurrencePeriod is required");
    }

    [Fact]
    public void ShouldContainErrorWhenEncounterPeriodMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Encounter.Period = null;

        var result = _sut.TestValidate(model);

        result.Errors.Should().Contain(e => e.ErrorMessage == "Encounter.Period is required");
    }

    [Fact]
    public void ShouldContainErrorWhenPatientAddressMissing()
    {
        var model = CreateValidModelFromExampleBundle();
        model.Patient.Address = [];

        var result = _sut.TestValidate(model);

        result.Errors.Should().Contain(e => e.ErrorMessage == "Patient.Address is required");
    }
}
