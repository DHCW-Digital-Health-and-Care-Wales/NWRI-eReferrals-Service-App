using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NWRI.eReferralsService.API.HealthChecks;
using NWRI.eReferralsService.API.Validators;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.Unit.Tests.HealthChecks;

public class FhirBundleProfileValidatorHealthCheckTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly Mock<IFhirBundleProfileValidator> _validatorMock;

    public FhirBundleProfileValidatorHealthCheckTests()
    {
        _validatorMock = _fixture.Mock<IFhirBundleProfileValidator>();
    }

    [Fact]
    public async Task CheckHealthAsyncWhenValidatorNotInitializedShouldReturnUnhealthy()
    {
        // Arrange
        _validatorMock.SetupGet(x => x.IsInitialized).Returns(false);
        _validatorMock.SetupGet(x => x.IsReady).Returns(false);

        var sut = CreateHealthCheck();

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("FHIR Validator failed to initialize.");
    }

    [Fact]
    public async Task CheckHealthAsyncWhenValidatorInitializedAndReadyShouldReturnHealthy()
    {
        // Arrange
        _validatorMock.SetupGet(x => x.IsInitialized).Returns(true);
        _validatorMock.SetupGet(x => x.IsReady).Returns(true);

        var sut = CreateHealthCheck();

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("FHIR Bundle Profile Validator is ready.");
    }

    [Fact]
    public async Task CheckHealthAsyncWhenValidatorInitializedButNotReadyShouldReturnDegraded()
    {
        // Arrange
        _validatorMock.SetupGet(x => x.IsInitialized).Returns(true);
        _validatorMock.SetupGet(x => x.IsReady).Returns(false);

        var sut = CreateHealthCheck();

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("FHIR Bundle Profile Validator is warming up.");
    }

    private FhirBundleProfileValidatorHealthCheck CreateHealthCheck()
    {
        return new FhirBundleProfileValidatorHealthCheck(_validatorMock.Object);
    }
}
