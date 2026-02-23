using Microsoft.Extensions.FileProviders;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.API.Services;

public class StaticFileCapabilityStatementService : ICapabilityStatementService
{
    private const string ResourcePath = "Swagger/Examples/metadata-capability-statement-response.json";

    private readonly IFileProvider _files;

    public StaticFileCapabilityStatementService(IWebHostEnvironment env)
    {
        _files = env.ContentRootFileProvider;
    }

    public async Task<string> GetCapabilityStatementAsync(CancellationToken cancellationToken)
    {
        var fileInfo = _files.GetFileInfo(ResourcePath);

        if (!fileInfo.Exists)
        {
            var ex = new FileNotFoundException("CapabilityStatement JSON file not found", fileInfo.PhysicalPath);
            throw new CapabilityStatementUnavailableException(ex);
        }

        await using var stream = fileInfo.CreateReadStream();
        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync(cancellationToken);
    }
}
