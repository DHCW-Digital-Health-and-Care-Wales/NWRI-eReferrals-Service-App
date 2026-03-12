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
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.Unit.Tests.Validators;

public class BundleCancelReferralModelValidatorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly BundleCancelReferralModelValidator _sut;
    private const string CancelBundleFile = "example-cancel-revoked.json";
    private const string DeleteBundleFile = "example-cancel-entered-in-error.json";

    public BundleCancelReferralModelValidatorTests()
    {
        _sut = _fixture.CreateWithFrozen<BundleCancelReferralModelValidator>();
        _sut.ClassLevelCascadeMode = CascadeMode.Continue;
    }

    private static BundleCancelReferralModel CreateValidModelFromExampleBundle(string fileName)
    {
        var bundleJson = File.ReadAllText($"TestData/{fileName}");
        var options = new JsonSerializerOptions()
            .ForFhir(ModelInfo.ModelInspector);

        var bundle = JsonSerializer.Deserialize<Bundle>(bundleJson, options)!;
        return BundleCancelReferralModel.FromBundle(bundle);
    }

    [Fact]
    public void CancelStatusBundleShouldBeValid()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EnteredInErrorStatusBundleShouldBeValid()
    {
        var model = CreateValidModelFromExampleBundle(DeleteBundleFile);

        var result = _sut.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldContainErrorWhenMessageHeaderNull()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.MessageHeader = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.MessageHeader)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(MessageHeader)));
    }

    [Fact]
    public void ShouldContainErrorWhenServiceRequestNull()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.ServiceRequest = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ServiceRequest)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(ServiceRequest)));
    }

    [Fact]
    public void ShouldContainErrorWhenPatientNull()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.Patient = null;

        var result = _sut.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Patient)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(Patient)));
    }

    [Fact]
    public void ShouldContainErrorWhenOrganizationsNull()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.Organizations = null;

        var result = _sut.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Organizations)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(Organization)));
    }

    [Fact]
    public void ShouldContainErrorWhenPatientNhsNumberMissing()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.Patient!.Identifier =
        [
            new Identifier
            {
                System = "https://example.org/local-patient-id",
                Value = "ABC123"
            }
        ];

        var result = _sut.TestValidate(model);
        result.Errors.Should().Contain(e => e.ErrorMessage == "Patient NHS number identifier is required");
    }

    [Fact]
    public void ShouldContainErrorWhenServiceRequestOccurrencePeriodMissing()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.ServiceRequest!.Occurrence = null;

        var result = _sut.TestValidate(model);
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == ValidationMessages.MissingEntityField<ServiceRequest>("occurrencePeriod"));
    }

    [Fact]
    public void ShouldContainErrorWhenOrganizationsMissing()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.Organizations = [];

        var result = _sut.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Organizations)
            .WithErrorMessage(ValidationMessages.MissingBundleEntity(nameof(Organization)));
    }

    [Fact]
    public void ShouldContainErrorWhenOrganizationIdentifierMissing()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.Organizations![0].Identifier = null;

        var result = _sut.TestValidate(model);
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == ValidationMessages.MissingEntityField<Organization>(nameof(Organization.Identifier)));
    }

    [Fact]
    public void ShouldContainErrorWhenMessageHeaderSenderMissing()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.MessageHeader!.Sender = null;

        var result = _sut.TestValidate(model);
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == ValidationMessages.MissingEntityField<MessageHeader>(nameof(MessageHeader.Sender)));
    }

    [Fact]
    public void ShouldContainErrorWhenServiceRequestMetaMissing()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.ServiceRequest!.Meta = null;

        var result = _sut.TestValidate(model);
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == ValidationMessages.MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Meta)));
    }

    [Fact]
    public void ShouldContainErrorWhenPatientIdentifierMissing()
    {
        var model = CreateValidModelFromExampleBundle(CancelBundleFile);
        model.Patient!.Identifier = [];

        var result = _sut.TestValidate(model);
        result.Errors.Should().Contain(e =>
            e.ErrorMessage == ValidationMessages.MissingEntityField<Patient>(nameof(Patient.Identifier)));
    }
}
