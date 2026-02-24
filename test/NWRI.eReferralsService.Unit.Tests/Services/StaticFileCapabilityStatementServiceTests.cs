using System.Text;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Moq;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Services;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class StaticFileCapabilityStatementServiceTests
{
    private const string ResourcePath = "Resources/Fhir/metadata-capability-statement-response.json";

    [Fact]
    public async Task GetCapabilityStatementAsyncShouldReturnJsonWhenFileExists()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement"}""";
        var fileInfo = new TestFileInfo
        {
            Exists = true,
            PhysicalPath = @"C:\fake\metadata-capability-statement-response.json",
            StreamFactory = () => new MemoryStream(Encoding.UTF8.GetBytes(json))
        };

        var (sut, fileProviderMock) = CreateSut(fileInfo);

        // Act
        var result = await sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        result.Should().Be(json);
        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);
        fileInfo.CreateReadStreamCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCapabilityStatementAsyncShouldThrowWhenFileMissing()
    {
        // Arrange
        var fileInfo = new TestFileInfo
        {
            Exists = false,
            PhysicalPath = @"C:\missing.json"
        };

        var (sut, fileProviderMock) = CreateSut(fileInfo);

        // Act
        var act = async () => await sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<CapabilityStatementUnavailableException>();
        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);

        ex.Which.Cause.Should().BeOfType<FileNotFoundException>();
        ex.Which.Cause.Message.Should().Contain("CapabilityStatement JSON file not found");
    }

    [Fact]
    public async Task GetCapabilityStatementAsyncShouldCacheResult()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement"}""";
        var fileInfo = new TestFileInfo
        {
            Exists = true,
            PhysicalPath = @"C:\fake\metadata-capability-statement-response.json",
            StreamFactory = () => new MemoryStream(Encoding.UTF8.GetBytes(json))
        };

        var (sut, fileProviderMock) = CreateSut(fileInfo);

        // Act
        var first = await sut.GetCapabilityStatementAsync(CancellationToken.None);
        var second = await sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        first.Should().Be(json);
        second.Should().Be(json);
        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);
        fileInfo.CreateReadStreamCallCount.Should().Be(1);
    }

    private static (StaticFileCapabilityStatementService Sut, Mock<IFileProvider> FileProviderMock) CreateSut(IFileInfo fileInfo)
    {
        var fileProviderMock = new Mock<IFileProvider>();
        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var sut = new StaticFileCapabilityStatementService(fileProviderMock.Object);

        return (sut, fileProviderMock);
    }

    private sealed class TestFileInfo : IFileInfo
    {
        public bool Exists { get; set; }
        public long Length => 0;
        public string? PhysicalPath { get; set; }
        public string Name => "metadata-capability-statement-response.json";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public bool IsDirectory => false;

        public Func<Stream> StreamFactory { get; set; } = () => new MemoryStream(Array.Empty<byte>());

        public int CreateReadStreamCallCount { get; private set; }

        public Stream CreateReadStream()
        {
            CreateReadStreamCallCount++;
            return StreamFactory();
        }
    }
}
