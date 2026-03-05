using System.Collections.Concurrent;
using Microsoft.Extensions.FileProviders;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.API.Services;

public class StaticFileCapabilityStatementService : ICapabilityStatementService
{
    private const string CapabilityStatementResponseJsonFilePath = "Resources/Fhir/metadata-capability-statement-response.json";
    private readonly ConcurrentDictionary<string, string> _capabilityStatementResponseCache = new();
    private readonly IFileProvider _fileProvider;

    public StaticFileCapabilityStatementService(IFileProvider fileProvider)
    {
        _fileProvider = fileProvider;
    }

    public async Task<string> GetCapabilityStatementAsync(CancellationToken cancellationToken)
    {
        if (_capabilityStatementResponseCache.TryGetValue(CapabilityStatementResponseJsonFilePath, out var cached))
        {
            return cached;
        }

        var result = await LoadResourceAsync(CapabilityStatementResponseJsonFilePath, cancellationToken);
        _capabilityStatementResponseCache.TryAdd(CapabilityStatementResponseJsonFilePath, result);

        return result;
    }

    private async Task<string> LoadResourceAsync(string resourcePath, CancellationToken cancellationToken)
    {
        var fileInfo = _fileProvider.GetFileInfo(resourcePath);

        if (!fileInfo.Exists)
        {
            throw new CapabilityStatementUnavailableException(resourcePath, "File does not exist");
        }

        try
        {
            await using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new CapabilityStatementUnavailableException(resourcePath, ex.Message);
        }
    }
}
