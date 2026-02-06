using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Serialization;
using NWRI.eReferralsService.API.Validators;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.Unit.Tests.Validators;

public class FhirBundleProfileValidatorTests
{
    private readonly IFhirJsonSerializerOptions _jsonSerializerOptions = new FhirJsonSerializerOptions();

    [Fact]
    public async Task ValidateShouldReturnSuccessfulOutputWhenDisabled()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = false
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(
            config,
            hostEnvironment.Object,
            NullLogger<FhirBundleProfileValidator>.Instance,
            _jsonSerializerOptions);

        // Act
        var output = await sut.ValidateAsync(new Bundle { Type = Bundle.BundleType.Message });

        // Assert
        output.IsSuccessful.Should().BeTrue();
        output.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateShouldThrowWhenEnabledButNotInitialized()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(
            config,
            hostEnvironment.Object,
            NullLogger<FhirBundleProfileValidator>.Instance,
            _jsonSerializerOptions);

        // Act
        var action = async () => await sut.ValidateAsync(new Bundle { Type = Bundle.BundleType.Message });

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("FHIR Validator must be initialized before use.");
    }

    [Fact]
    public void IsInitializedShouldBeFalseBeforeInitialization()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(
            config,
            hostEnvironment.Object,
            NullLogger<FhirBundleProfileValidator>.Instance,
            _jsonSerializerOptions);

        // Assert
        sut.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void IsReadyShouldBeFalseBeforeInitialization()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(
            config,
            hostEnvironment.Object,
            NullLogger<FhirBundleProfileValidator>.Instance,
            _jsonSerializerOptions);

        // Assert
        sut.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeShouldThrowWhenEnabledAndNoPackageFilesFoundInDirectory()
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
            var sut = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance,
                _jsonSerializerOptions);

            // Act
            var action = async () => await sut.InitializeAsync();

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
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
    public async Task InitializeShouldThrowWhenEnabledAndPackageDirectoryDoesNotExist()
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
            var sut = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance,
                _jsonSerializerOptions);

            // Act
            var action = async () => await sut.InitializeAsync();

            // Assert
            await action.Should().ThrowAsync<InvalidOperationException>()
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

    [Fact]
    public async Task ValidateShouldThrowWhenValidationTimeoutIsExceeded()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true,
            ValidationTimeoutSeconds = 0 // Set to 0 to force immediate timeout
        });

        var contentRoot = SetupTestEnvironmentWithPackages();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

        try
        {
            var sut = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance,
                _jsonSerializerOptions);

            await sut.InitializeAsync();

            // Act
            var action = async () => await sut.ValidateAsync(new Bundle { Type = Bundle.BundleType.Message });

            // Assert
            await action.Should().ThrowAsync<TimeoutException>()
                .WithMessage("*timed out*");
        }
        finally
        {
            CleanupTestEnvironment(contentRoot);
        }
    }

    [Fact]
    public async Task ValidateShouldThrowWhenCancellationTokenIsCancelled()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var contentRoot = SetupTestEnvironmentWithPackages();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

        try
        {
            var sut = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance,
                _jsonSerializerOptions);

            await sut.InitializeAsync();

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var cancelledToken = cts.Token;

            // Act
            var action = async () => await sut.ValidateAsync(new Bundle { Type = Bundle.BundleType.Message }, cancelledToken);

            // Assert
            await action.Should().ThrowAsync<OperationCanceledException>();
        }
        finally
        {
            CleanupTestEnvironment(contentRoot);
        }
    }

    [Fact]
    public void DisposeShouldNotThrowException()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = false
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(
            config,
            hostEnvironment.Object,
            NullLogger<FhirBundleProfileValidator>.Instance,
            _jsonSerializerOptions);

        // Act
        var action = () => sut.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void DisposeShouldNotThrowExceptionWhenCalledMultipleTimes()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = false
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(
            config,
            hostEnvironment.Object,
            NullLogger<FhirBundleProfileValidator>.Instance,
            _jsonSerializerOptions);

        // Act
        var action = () =>
        {
            sut.Dispose();
            sut.Dispose();
        };

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task DisposeShouldNotThrowExceptionAfterInitialization()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var contentRoot = SetupTestEnvironmentWithPackages();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

        try
        {
            var sut = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance,
                _jsonSerializerOptions);

            await sut.InitializeAsync();

            // Act
            var action = () => sut.Dispose();

            // Assert
            action.Should().NotThrow();
        }
        finally
        {
            CleanupTestEnvironment(contentRoot);
        }
    }

    [Fact]
    public async Task InitializeShouldSetIsInitializedAndIsReadyToTrue()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var contentRoot = SetupTestEnvironmentWithPackages();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

        try
        {
            var sut = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance,
                _jsonSerializerOptions);

            // Act
            await sut.InitializeAsync();

            // Assert
            sut.IsInitialized.Should().BeTrue();
            sut.IsReady.Should().BeTrue();
        }
        finally
        {
            CleanupTestEnvironment(contentRoot);
        }
    }

    [Fact]
    public async Task InitializeShouldContinueWhenWarmupExampleFileDoesNotExist()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = true
        });

        var contentRoot = SetupTestEnvironmentWithPackages();
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(contentRoot);

        try
        {
            var sut = new FhirBundleProfileValidator(
                config,
                hostEnvironment.Object,
                NullLogger<FhirBundleProfileValidator>.Instance,
                _jsonSerializerOptions);

            // Act
            await sut.InitializeAsync();

            // Assert
            sut.IsInitialized.Should().BeTrue();
            sut.IsReady.Should().BeTrue();
        }
        finally
        {
            CleanupTestEnvironment(contentRoot);
        }
    }

    [Fact]
    public async Task ValidateShouldReturnSuccessfulOutputWhenDisabledWithoutInitialization()
    {
        // Arrange
        var config = Options.Create(new FhirBundleProfileValidationConfig
        {
            Enabled = false
        });

        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.SetupGet(x => x.ContentRootPath).Returns(Path.GetTempPath());

        var sut = new FhirBundleProfileValidator(
            config,
            hostEnvironment.Object,
            NullLogger<FhirBundleProfileValidator>.Instance,
            _jsonSerializerOptions);

        // Act
        var output = await sut.ValidateAsync(new Bundle { Type = Bundle.BundleType.Message });

        // Assert
        output.IsSuccessful.Should().BeTrue();
        output.Errors.Should().BeEmpty();
        sut.IsInitialized.Should().BeFalse();
        sut.IsReady.Should().BeFalse();
    }

    private static string SetupTestEnvironmentWithPackages()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var packageDir = Path.Combine(contentRoot, "FhirPackages");
        Directory.CreateDirectory(packageDir);

        // Create a dummy package file
        var dummyPackageFile = Path.Combine(packageDir, "dummy-package.tgz");
        File.WriteAllText(dummyPackageFile, "dummy content");

        return contentRoot;
    }

    private static void CleanupTestEnvironment(string contentRoot)
    {
        if (Directory.Exists(contentRoot))
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }
}
