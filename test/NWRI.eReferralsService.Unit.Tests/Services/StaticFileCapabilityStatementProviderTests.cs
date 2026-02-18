using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Moq;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Services;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class StaticFileCapabilityStatementProviderTests
{
    private const string ResourcePath = "Resources/Fhir/BaRS-Compatibility-Statement.json";

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncWhenFileExistsReturnsJson()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement","status":"active"}""";
        var (provider, fileProviderMock, fileInfo) = CreateSutWithFile(json);

        // Act
        var result = await provider.GetCapabilityStatementJsonAsync(CancellationToken.None);

        // Assert
        Assert.Equal(json, result);
        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);
        Assert.True(fileInfo.StreamDisposed);
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncCachesResultSecondCallDoesNotTouchFileSystem()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement"}""";
        var (provider, fileProviderMock, fileInfo) = CreateSutWithFile(json);

        // Act
        var first = await provider.GetCapabilityStatementJsonAsync(CancellationToken.None);
        var second = await provider.GetCapabilityStatementJsonAsync(CancellationToken.None);

        // Assert
        Assert.Equal(json, first);
        Assert.Equal(json, second);

        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);
        Assert.Equal(1, fileInfo.CreateReadStreamCallCount);
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncWhenFileMissingThrowsCapabilityStatementUnavailableException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementProvider>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo
        {
            Exists = false,
            PhysicalPath = "C:\\missing.json"
        };

        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementProvider(envMock.Object, loggerMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<CapabilityStatementUnavailableException>(() =>
            sut.GetCapabilityStatementJsonAsync(CancellationToken.None));

        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Once);

        VerifyLogged(loggerMock, LogLevel.Error, "CapabilityStatement JSON file not found", Times.Once());
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncWhenFileMissingPhysicalPathNullStillThrowsAndLogs()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementProvider>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo { Exists = false, PhysicalPath = null };
        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementProvider(envMock.Object, loggerMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<CapabilityStatementUnavailableException>(() =>
            sut.GetCapabilityStatementJsonAsync(CancellationToken.None));

        VerifyLogged(loggerMock, LogLevel.Error, "CapabilityStatement JSON file not found", Times.Once());
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncWhenFileMissingDoesNotCacheNextCallTriesAgain()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementProvider>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo { Exists = false, PhysicalPath = "x" };
        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementProvider(envMock.Object, loggerMock.Object);

        // Act
        await Assert.ThrowsAsync<CapabilityStatementUnavailableException>(() =>
            sut.GetCapabilityStatementJsonAsync(CancellationToken.None));

        await Assert.ThrowsAsync<CapabilityStatementUnavailableException>(() =>
            sut.GetCapabilityStatementJsonAsync(CancellationToken.None));

        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Exactly(2));
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncWhenCreateReadStreamThrowsPropagatesAndDoesNotCache()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementProvider>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo
        {
            Exists = true,
            PhysicalPath = "x",
            StreamFactory = () => throw new IOException("boom")
        };

        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementProvider(envMock.Object, loggerMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<IOException>(() =>
            sut.GetCapabilityStatementJsonAsync(CancellationToken.None));

        await Assert.ThrowsAsync<IOException>(() =>
            sut.GetCapabilityStatementJsonAsync(CancellationToken.None));

        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Exactly(2));
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncWhenCancelledThrowsOperationCanceledExceptionAndDoesNotCache()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement"}""";
        var (sut, fileProviderMock, fileInfo) = CreateSutWithFile(json);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.GetCapabilityStatementJsonAsync(cts.Token));

        var result = await sut.GetCapabilityStatementJsonAsync(CancellationToken.None);
        Assert.Equal(json, result);

        fileProviderMock.Verify(fp => fp.GetFileInfo(ResourcePath), Times.Exactly(2));
        Assert.True(fileInfo.StreamDisposed);
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncDisposesStreamEvenOnReadFailure()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementProvider>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var stream = new ThrowOnReadStream();
        var fileInfo = new TestFileInfo
        {
            Exists = true,
            PhysicalPath = "x",
            StreamFactory = () => stream
        };

        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementProvider(envMock.Object, loggerMock.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetCapabilityStatementJsonAsync(CancellationToken.None));

        Assert.True(stream.Disposed);
    }

    [Fact]
    public async Task GetCapabilityStatementJsonAsyncConcurrentCallsDoNotDeadlockAndReturnSameJson()
    {
        // Arrange
        var json = """{"resourceType":"CapabilityStatement"}""";
        var (sut, _, _) = CreateSutWithFile(json);

        // Act
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => sut.GetCapabilityStatementJsonAsync(CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.Equal(json, r));
    }

    private static (StaticFileCapabilityStatementProvider Sut, Mock<IFileProvider> FileProviderMock, TestFileInfo FileInfo)
        CreateSutWithFile(string json)
    {
        var loggerMock = new Mock<ILogger<StaticFileCapabilityStatementProvider>>();
        var fileProviderMock = new Mock<IFileProvider>();

        var fileInfo = new TestFileInfo
        {
            Exists = true,
            PhysicalPath = "C:\\fake\\BaRS-Compatibility-Statement.json",
            StreamFactory = () => new TrackDisposeMemoryStream(Encoding.UTF8.GetBytes(json))
        };

        fileProviderMock.Setup(fp => fp.GetFileInfo(ResourcePath)).Returns(fileInfo);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootFileProvider).Returns(fileProviderMock.Object);

        var sut = new StaticFileCapabilityStatementProvider(envMock.Object, loggerMock.Object);
        return (sut, fileProviderMock, fileInfo);
    }

    private static void VerifyLogged<T>(Mock<ILogger<T>> logger, LogLevel level, string contains, Times times)
    {
        logger.Verify(
            x => x.Log(
            level,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.Is<FileNotFoundException>(ex => ex.Message.Contains(contains)),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
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
        public bool StreamDisposed => _lastStream is TrackDisposeMemoryStream td && td.Disposed;

        private Stream? _lastStream;

        public Stream CreateReadStream()
        {
            CreateReadStreamCallCount++;
            _lastStream = StreamFactory();
            return _lastStream;
        }
    }

    private sealed class TrackDisposeMemoryStream : MemoryStream
    {
        public bool Disposed { get; private set; }
        public TrackDisposeMemoryStream(byte[] buffer) : base(buffer) { }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class ThrowOnReadStream : MemoryStream
    {
        public bool Disposed { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("read failed");
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
