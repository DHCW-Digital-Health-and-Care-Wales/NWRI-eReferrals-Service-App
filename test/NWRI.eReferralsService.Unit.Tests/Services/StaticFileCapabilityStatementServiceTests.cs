using System.Text;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Moq;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using Task = System.Threading.Tasks.Task;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class StaticFileCapabilityStatementServiceTests
{
    private const string ExpectedResourcePath = "Resources/Fhir/metadata-capability-statement-response.json";
    private const string ValidJsonContent = """{"resourceType":"CapabilityStatement","status":"active"}""";

    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly Mock<IFileProvider> _fileProviderMock;

    public StaticFileCapabilityStatementServiceTests()
    {
        _fileProviderMock = _fixture.Mock<IFileProvider>();
    }

    private StaticFileCapabilityStatementService CreateSut()
    {
        return new StaticFileCapabilityStatementService(_fileProviderMock.Object);
    }

    [Fact]
    public async Task ShouldReturnContentWhenFileExists()
    {
        // Arrange
        SetupFileProvider(exists: true, content: ValidJsonContent);
        var sut = CreateSut();

        // Act
        var result = await sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        result.Should().Be(ValidJsonContent);
    }

    [Fact]
    public async Task ShouldThrowWhenFileDoesNotExist()
    {
        // Arrange
        SetupFileProvider(exists: false);
        var sut = CreateSut();

        // Act
        var act = () => sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<CapabilityStatementUnavailableException>();

        ex.Which.Message.Should().Contain("CapabilityStatement");
        ex.Which.Errors.Should().ContainSingle();
        ex.Which.Errors.Single().DiagnosticsMessage.Should().Contain(ExpectedResourcePath);
        ex.Which.Errors.Single().DiagnosticsMessage.Should().Contain("File does not exist");
    }

    [Fact]
    public async Task ShouldThrowWhenStreamFails()
    {
        // Arrange
        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.Setup(f => f.Exists).Returns(true);
        fileInfoMock.Setup(f => f.CreateReadStream()).Throws(new IOException("disk read failure"));

        _fileProviderMock.Setup(fp => fp.GetFileInfo(ExpectedResourcePath)).Returns(fileInfoMock.Object);

        var sut = CreateSut();

        // Act
        var act = () => sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<CapabilityStatementUnavailableException>();

        ex.Which.Errors.Should().ContainSingle();
        ex.Which.Errors.Single().DiagnosticsMessage.Should().Contain(ExpectedResourcePath);
        ex.Which.Errors.Single().DiagnosticsMessage.Should().Contain("disk read failure");
    }

    [Fact]
    public async Task ShouldReturnCachedResultOnSubsequentCalls()
    {
        // Arrange
        SetupFileProvider(exists: true, content: ValidJsonContent);
        var sut = CreateSut();

        // Act
        var first = await sut.GetCapabilityStatementAsync(CancellationToken.None);
        var second = await sut.GetCapabilityStatementAsync(CancellationToken.None);

        // Assert
        first.Should().Be(ValidJsonContent);
        second.Should().Be(ValidJsonContent);

        _fileProviderMock.Verify(fp => fp.GetFileInfo(ExpectedResourcePath), Times.Once);
    }

    [Fact]
    public async Task ShouldReturnSameResultUnderConcurrentAccess()
    {
        // Arrange
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.Setup(f => f.Exists).Returns(true);
        fileInfoMock.Setup(f => f.CreateReadStream()).Returns(() =>
        {
            barrier.Task.GetAwaiter().GetResult();
            return new MemoryStream(Encoding.UTF8.GetBytes(ValidJsonContent));
        });

        _fileProviderMock.Setup(fp => fp.GetFileInfo(ExpectedResourcePath)).Returns(fileInfoMock.Object);

        var sut = CreateSut();
        const int concurrentCalls = 10;

        // Act
        var tasks = Enumerable.Range(0, concurrentCalls)
            .Select(_ => Task.Run(() => sut.GetCapabilityStatementAsync(CancellationToken.None)))
            .ToArray();

        barrier.SetResult();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBeEquivalentTo(ValidJsonContent);
    }

    private void SetupFileProvider(bool exists, string content = "")
    {
        var fileInfoMock = new Mock<IFileInfo>();
        fileInfoMock.Setup(f => f.Exists).Returns(exists);

        if (exists)
        {
            fileInfoMock.Setup(f => f.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }

        _fileProviderMock.Setup(fp => fp.GetFileInfo(ExpectedResourcePath)).Returns(fileInfoMock.Object);
    }
}
