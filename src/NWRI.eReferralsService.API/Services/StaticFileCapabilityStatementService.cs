using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.API.Services;

public class StaticFileCapabilityStatementService : ICapabilityStatementService
{
    private const string ResourcePath = "Resources/Fhir/metadata-capability-statement-response.json";
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly IFileProvider _files;

    public StaticFileCapabilityStatementService(IFileProvider files)
    {
        _files = files;
    }

    public async Task<string> GetCapabilityStatementAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(ResourcePath, out var cached))
        {
            return cached;
        }

        var result = await LoadResourceAsync(ResourcePath, cancellationToken);
        _cache.TryAdd(ResourcePath, result);

        return result;
    }

    private async Task<string> LoadResourceAsync(string resourcePath, CancellationToken cancellationToken)
    {
        var fileInfo = _files.GetFileInfo(resourcePath);

        if (!fileInfo.Exists)
        {
            throw new CapabilityStatementUnavailableException(
                new CapabilityStatementNotFoundError(resourcePath,
                    "File does not exist"));
        }

        try
        {
            await using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CapabilityStatementUnavailableException(
                new CapabilityStatementLoadError(resourcePath, ex.Message));
        }
    }
}
