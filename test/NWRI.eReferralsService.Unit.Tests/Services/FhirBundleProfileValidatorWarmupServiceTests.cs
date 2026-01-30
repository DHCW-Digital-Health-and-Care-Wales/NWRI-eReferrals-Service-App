using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.API.Validators;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class FhirBundleProfileValidatorWarmupServiceTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly Mock<ILogger<FhirBundleProfileValidatorWarmupService>> _loggerMock;
    private readonly Mock<IFhirBundleProfileValidator> _validatorMock;

    public FhirBundleProfileValidatorWarmupServiceTests()
    {
        _loggerMock = _fixture.Mock<ILogger<FhirBundleProfileValidatorWarmupService>>();
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _validatorMock = _fixture.Mock<IFhirBundleProfileValidator>();
    }

    [Fact]
    public async Task StartAsyncWhenValidationEnabledShouldCallInitializeAsync()
    {
        // Arrange
        var config = new FhirBundleProfileValidationConfig { Enabled = true };
        _fixture.Mock<IOptions<FhirBundleProfileValidationConfig>>()
            .SetupGet(x => x.Value)
            .Returns(config);

        var sut = CreateService();

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _validatorMock.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsyncWhenValidationDisabledShouldNotCallInitializeAsync()
    {
        // Arrange
        var config = new FhirBundleProfileValidationConfig { Enabled = false };
        _fixture.Mock<IOptions<FhirBundleProfileValidationConfig>>()
            .SetupGet(x => x.Value)
            .Returns(config);

        var sut = CreateService();

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _validatorMock.Verify(x => x.InitializeAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsyncWhenValidationEnabledShouldLogStartedAndComplete()
    {
        // Arrange
        var config = new FhirBundleProfileValidationConfig { Enabled = true };
        _fixture.Mock<IOptions<FhirBundleProfileValidationConfig>>()
            .SetupGet(x => x.Value)
            .Returns(config);

        var sut = CreateService();

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Name == "WarmupFhirBundleProfileValidationStarted"),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.Is<EventId>(e => e.Name == "WarmupFhirBundleProfileValidationComplete"),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsyncWhenValidationDisabledShouldLogWarning()
    {
        // Arrange
        var config = new FhirBundleProfileValidationConfig { Enabled = false };
        _fixture.Mock<IOptions<FhirBundleProfileValidationConfig>>()
            .SetupGet(x => x.Value)
            .Returns(config);

        var sut = CreateService();

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.Is<EventId>(e => e.Name == "WarmupFhirBundleProfileValidationDisabled"),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsyncWhenCancellationRequestedShouldPassCancellationTokenToValidator()
    {
        // Arrange
        var config = new FhirBundleProfileValidationConfig { Enabled = true };
        _fixture.Mock<IOptions<FhirBundleProfileValidationConfig>>()
            .SetupGet(x => x.Value)
            .Returns(config);

        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        var sut = CreateService();

        // Act
        await sut.StartAsync(expectedToken);

        // Assert
        _validatorMock.Verify(x => x.InitializeAsync(expectedToken), Times.Once);
    }

    [Fact]
    public async Task StopAsyncShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new FhirBundleProfileValidationConfig { Enabled = true };
        _fixture.Mock<IOptions<FhirBundleProfileValidationConfig>>()
            .SetupGet(x => x.Value)
            .Returns(config);

        var sut = CreateService();

        // Act
        var action = async () => await sut.StopAsync(CancellationToken.None);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsyncShouldReturnCompletedTask()
    {
        // Arrange
        var config = new FhirBundleProfileValidationConfig { Enabled = true };
        _fixture.Mock<IOptions<FhirBundleProfileValidationConfig>>()
            .SetupGet(x => x.Value)
            .Returns(config);

        var sut = CreateService();

        // Act
        var task = sut.StopAsync(CancellationToken.None);

        // Assert
        task.IsCompleted.Should().BeTrue();
        await task;
    }

    private FhirBundleProfileValidatorWarmupService CreateService()
    {
        return new FhirBundleProfileValidatorWarmupService(
            _loggerMock.Object,
            _validatorMock.Object,
            _fixture.Freeze<IOptions<FhirBundleProfileValidationConfig>>());
    }
}
