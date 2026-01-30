using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NWRI.eReferralsService.API.HealthChecks;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.Unit.Tests.HealthChecks;

public class ApplicationLivenessHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsyncShouldReturnHealthy()
    {
        // Arrange
        var sut = new ApplicationLivenessHealthCheck();

        // Act
        var result = await sut.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Application is running.");
    }
}
