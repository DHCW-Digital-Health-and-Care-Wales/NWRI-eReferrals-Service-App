using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WCCG.eReferralsService.API.Configuration;
using WCCG.eReferralsService.API.Validators;

namespace WCCG.eReferralsService.Unit.Tests.Validators;

public class FhirBundleProfileValidatorTests
{
    [Fact]
    public void ValidateShouldReturnSuccessfulOutputWhenDisabled()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = false
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(config, hostEnvironment.Object, NullLogger<FhirBundleProfileValidator>.Instance);

        // Act
        var output = sut.Validate(new Bundle { Type = Bundle.BundleType.Message });

        // Assert
        output.IsSuccessful.Should().BeTrue();
        output.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateShouldThrowWhenEnabledAndNoPackageFilesFoundInDirectory()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var packageDir = Path.Combine(contentRoot, "FhirPackages");
        Directory.CreateDirectory(packageDir);

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

        try
        {
            // Act
            var action = () => _ = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance);

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*no package files were found*");
        }
        finally
        {
            if (Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void ValidateShouldThrowWhenEnabledAndPackageDirectoryDoesNotExist()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

        try
        {
            // Act
            var action = () => _ = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance);

            // Assert
            action.Should().Throw<InvalidOperationException>()
                .WithMessage("*package directory*does not exist*");
        }
        finally
        {
            if (Directory.Exists(contentRoot))
            {
                Directory.Delete(contentRoot, recursive: true);
            }
        }
    }
}
