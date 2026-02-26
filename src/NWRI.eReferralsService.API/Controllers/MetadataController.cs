using Microsoft.AspNetCore.Mvc;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Middleware;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.API.Swagger.Attributes;

namespace NWRI.eReferralsService.API.Controllers;

[ApiController]
[AuditLogRequest]
public sealed class MetadataController : ControllerBase
{
    private readonly ICapabilityStatementService _capabilityStatementService;
    private readonly ILogger<MetadataController> _logger;

    public MetadataController(ICapabilityStatementService capabilityStatementService, ILogger<MetadataController> logger)
    {
        _capabilityStatementService = capabilityStatementService;
        _logger = logger;
    }

    [HttpGet("/metadata")]
    [SwaggerGetMetadataRequest]
    public async Task<IActionResult> GetMetadata(CancellationToken cancellationToken)
    {
        _logger.CalledMethod(nameof(GetMetadata));

        var json = await _capabilityStatementService.GetCapabilityStatementAsync(cancellationToken);

        return new ContentResult
        {
            Content = json,
            StatusCode = StatusCodes.Status200OK,
            ContentType = FhirConstants.FhirMediaType
        };
    }
}
