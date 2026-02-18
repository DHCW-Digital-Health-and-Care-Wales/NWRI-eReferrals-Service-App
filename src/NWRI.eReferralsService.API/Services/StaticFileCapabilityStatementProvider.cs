using Microsoft.Extensions.FileProviders;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions.Logger;

namespace NWRI.eReferralsService.API.Services;

public sealed class StaticFileCapabilityStatementProvider : ICapabilityStatementProvider
{
    private const string ResourcePath = "Resources/Fhir/BaRS-Compatibility-Statement.json";

    private readonly IFileProvider _files;
    private readonly ILogger<StaticFileCapabilityStatementProvider> _logger;

    private string? _cached;
    private readonly object _lock = new();

    public StaticFileCapabilityStatementProvider(IWebHostEnvironment env,
        ILogger<StaticFileCapabilityStatementProvider> logger)
    {
        _files = env.ContentRootFileProvider;
        _logger = logger;
    }

    public async Task<string> GetCapabilityStatementJsonAsync(CancellationToken ct)
    {
        lock (_lock)
        {
            if (_cached is not null) return _cached;
        }

        var fileInfo = _files.GetFileInfo(ResourcePath);
        if (!fileInfo.Exists)
        {
            var inner = new FileNotFoundException("CapabilityStatement JSON file not found", fileInfo.PhysicalPath);

            _logger.CapabilityStatementJsonNotFound(ResourcePath, fileInfo.PhysicalPath, inner);

            throw new CapabilityStatementUnavailableException();
        }

        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);

        var json = await reader.ReadToEndAsync(ct);

        lock (_lock) _cached = json;

        return json;
    }
}

