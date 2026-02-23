using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Services;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class StaticFileCapabilityStatementServiceTests
{
    private const string ResourcePath = "Resources/Fhir/BaRS-Compatibility-Statement.json";

    [Fact]
    public async Task GetCapabilityStatementAsyncWhenFileExistsReturnsJsonAndCaches()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement"}""";
        var (sut, fileProviderMock, fileInfo) = CreateSutWithFile(json);

        // Act
        var first = await sut.GetCapabilityStatementAsync(CancellationToken.None);
        var second = await sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        Assert.Equal(json, first);
        Assert.Equal(json, second);

        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);
        Assert.Equal(1, fileInfo.CreateReadStreamCallCount);
    }

    [Fact]
    public async Task GetCapabilityStatementAsyncWhenFileMissingThrowsAndLogs()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementService>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo { Exists = false, PhysicalPath = "x" };
        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementService(envMock.Object, loggerMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<CapabilityStatementUnavailableException>(() =>
            sut.GetCapabilityStatementAsync(CancellationToken.None));

        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<FileNotFoundException>(ex => ex.Message.Contains("CapabilityStatement", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static (StaticFileCapabilityStatementService Sut, Mock<IFileProvider> FileProviderMock, TestFileInfo FileInfo)
        CreateSutWithFile(string json)
    {
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementService>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo
        {
            Exists = true,
            PhysicalPath = "C:\\fake\\BaRS-Compatibility-Statement.json",
            StreamFactory = () => new MemoryStream(Encoding.UTF8.GetBytes(json))
        };

        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementService(envMock.Object, loggerMock.Object);
        return (sut, fileProviderMock, fileInfo);
    }

    private sealed class TestFileInfo : IFileInfo
    {
        public bool Exists { get; set; }
        public long Length => 0;
        public string? PhysicalPath { get; set; }
        public string Name => "BaRS-Compatibility-Statement.json";
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
