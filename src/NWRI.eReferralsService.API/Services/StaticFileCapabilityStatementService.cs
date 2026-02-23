using Microsoft.Extensions.FileProviders;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions.Logger;

namespace NWRI.eReferralsService.API.Services;

public class StaticFileCapabilityStatementService : ICapabilityStatementService
{
    private const string ResourcePath = "Resources/Fhir/BaRS-Compatibility-Statement.json";

    private readonly IFileProvider _files;
    private readonly ILogger<StaticFileCapabilityStatementService> _logger;

    private string? _cached;
    private readonly object _lock = new();

    public StaticFileCapabilityStatementService(IWebHostEnvironment env,
        ILogger<StaticFileCapabilityStatementService> logger)
    {
        _files = env.ContentRootFileProvider;
        _logger = logger;
    }

    public async Task<string> GetCapabilityStatementAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_cached is not null) return _cached;
        }

        var fileInfo = _files.GetFileInfo(ResourcePath);
        if (!fileInfo.Exists)
        {
            var inner = new FileNotFoundException("CapabilityStatement JSON file not found", fileInfo.PhysicalPath);

            _logger.CapabilityStatementJsonNotFound(ResourcePath, inner);

            throw new CapabilityStatementUnavailableException();
        }

        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);

        var json = await reader.ReadToEndAsync(ct);

        lock (_lock) _cached = json;

        return json;
    }
}

