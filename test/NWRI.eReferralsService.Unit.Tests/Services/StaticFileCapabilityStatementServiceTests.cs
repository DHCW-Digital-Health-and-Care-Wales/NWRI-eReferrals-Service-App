using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Moq;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Services;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class StaticFileCapabilityStatementServiceTests
{
    private const string ResourcePath = "Swagger/Examples/metadata-capability-statement-response.json";

    [Fact]
    public async Task GetCapabilityStatementAsyncWhenFileExistsReturnsJson()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement"}""";
        var (sut, fileProviderMock, fileInfo) = CreateSutWithFile(json);

        // Act
        var result = await sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        Assert.Equal(json, result);
        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);
        Assert.Equal(1, fileInfo.CreateReadStreamCallCount);
    }

    [Fact]
    public async Task GetCapabilityStatementAsyncWhenFileMissingThrowsCapabilityStatementUnavailableExceptionWithFileNotFoundCause()
    {
        // Arrange
        var fileProviderMock = new Mock<IFileProvider>();
        var fileInfo = new TestFileInfo { Exists = false, PhysicalPath = "C:\\missing.json" };

        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementService(envMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<CapabilityStatementUnavailableException>(() =>
            sut.GetCapabilityStatementAsync(CancellationToken.None));

        // Assert
        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);
        Assert.IsType<FileNotFoundException>(ex.Cause);
        Assert.Contains("CapabilityStatement JSON file not found", ex.Cause.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static (StaticFileCapabilityStatementService Sut, Mock<IFileProvider> FileProviderMock, TestFileInfo FileInfo)
        CreateSutWithFile(string json)
    {
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo
        {
            Exists = true,
            PhysicalPath = "C:\\fake\\metadata-capability-statement-response.json",
            StreamFactory = () => new MemoryStream(Encoding.UTF8.GetBytes(json))
        };

        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementService(envMock.Object);
        return (sut, fileProviderMock, fileInfo);
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
