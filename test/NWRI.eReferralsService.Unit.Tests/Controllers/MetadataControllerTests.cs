using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Controllers;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Controllers;

public class MetadataControllerTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    private readonly MetadataController _sut;

    public MetadataControllerTests()
    {
        _fixture.OmitAutoProperties = true;
        _sut = _fixture.CreateWithFrozen<MetadataController>();
    }

    [Fact]
    public async Task GetMetadataShouldReturn200WithFhirContentTypeAndJson()
    {
        // Arrange
        var outputJson = _fixture.Create<string>();

        _fixture.Mock<ICapabilityStatementService>()
            .Setup(x => x.GetCapabilityStatementAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(outputJson);

        // Act
        var result = await _sut.GetMetadata(CancellationToken.None);

        // Assert
        var contentResult = result.Should().BeOfType<ContentResult>().Subject;
        contentResult.StatusCode.Should().Be(200);
        contentResult.Content.Should().Be(outputJson);
        contentResult.ContentType.Should().Be(FhirConstants.FhirMediaType);
    }

    [Fact]
    public async Task GetMetadataShouldPropagateExceptionsFromService()
    {
        // Arrange
        var ex = new FileNotFoundException(_fixture.Create<string>());

        _fixture.Mock<ICapabilityStatementService>()
            .Setup(x => x.GetCapabilityStatementAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        // Act
        Func<Task> act = async () => await _sut.GetMetadata(CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<FileNotFoundException>())
            .Which.Message.Should().Be(ex.Message);
    }
}
