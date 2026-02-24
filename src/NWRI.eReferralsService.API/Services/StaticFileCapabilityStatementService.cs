using Microsoft.Extensions.FileProviders;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.API.Services;

public class StaticFileCapabilityStatementService : ICapabilityStatementService, IDisposable
{
    private const string ResourcePath = "Resources/Fhir/metadata-capability-statement-response.json";

    private readonly IFileProvider _files;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private string? _cached;
    private bool _disposed;

    public StaticFileCapabilityStatementService(IFileProvider files)
    {
        _files = files;
    }

    public async Task<string> GetCapabilityStatementAsync(CancellationToken cancellationToken)
    {
        if (_cached is not null)
        {
            return _cached;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cached is not null)
            {
                return _cached;
            }

            var fileInfo = _files.GetFileInfo(ResourcePath);
            if (!fileInfo.Exists)
            {
                var ex = new FileNotFoundException("CapabilityStatement JSON file not found", fileInfo.PhysicalPath);

                throw new CapabilityStatementUnavailableException(ex, ResourcePath);
            }

            await using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);

            _cached = await reader.ReadToEndAsync(cancellationToken);
            return _cached;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
