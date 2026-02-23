using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Validators;
using NWRI.eReferralsService.Unit.Tests.TestFixtures;

namespace NWRI.eReferralsService.Integration.Tests.Services;

public class WpasJsonSchemaValidatorTests : IClassFixture<WpasJsonSchemaValidatorTests.SchemaValidatorFixture>
{
    private readonly WpasJsonSchemaValidator _sut;

    public WpasJsonSchemaValidatorTests(SchemaValidatorFixture fixture)
    {
        _sut = fixture.Sut;
    }

    [Fact]
    public void ValidateShouldReturnInvalidWhenPayloadViolatesSchema()
    {
        var payload = WpasCreateReferralRequestBuilder.CreateValid("invalid");

        var result = _sut.ValidateWpasCreateReferralRequest(payload);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeNull();
    }

    [Fact]
    public void ValidateShouldReturnValidForKnownGoodPayload()
    {
        var payload = WpasCreateReferralRequestBuilder.CreateValid();
        var result = _sut.ValidateWpasCreateReferralRequest(payload);

        result.IsValid.Should().BeTrue();
    }

    public sealed class SchemaValidatorFixture
    {
        public WpasJsonSchemaValidator Sut { get; }

        public SchemaValidatorFixture()
        {
            var hostEnvironment = new TestHostEnvironment(AppContext.BaseDirectory);
            Sut = new WpasJsonSchemaValidator(hostEnvironment);
        }

        private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Development;
            public string ApplicationName { get; set; } = "IntegrationTests";
            public string ContentRootPath { get; set; } = contentRootPath;
            public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        }
    }
}
