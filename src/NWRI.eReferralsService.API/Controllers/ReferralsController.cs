using Microsoft.AspNetCore.Mvc;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.API.Swagger;

namespace NWRI.eReferralsService.API.Controllers;

[ApiController]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referralService;
    private readonly ILogger<ReferralsController> _logger;

    public ReferralsController(IReferralService referralService, ILogger<ReferralsController> logger)
    {
        _referralService = referralService;
        _logger = logger;
    }

    [HttpPost("/$process-message")]
    [SwaggerProcessMessageRequest]
    public async Task<IActionResult> ProcessMessage(CancellationToken cancellationToken)
    {
        _logger.CalledMethod(nameof(ProcessMessage));

        using var reader = new StreamReader(HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);

        var outputBundleJson = await _referralService.ProcessMessageAsync(HttpContext.Request.Headers, body, cancellationToken);

        return new ContentResult
        {
            Content = outputBundleJson,
            StatusCode = 200,
            ContentType = FhirConstants.FhirMediaType
        };
    }

    [HttpGet("ServiceRequest/{id}")]
    [SwaggerGetReferralRequest]
    public async Task<IActionResult> GetReferral(string? id)
    {
        _logger.CalledMethod(nameof(GetReferral));

        var outputBundleJson = await _referralService.GetReferralAsync(HttpContext.Request.Headers, id);

        return new ContentResult
        {
            Content = outputBundleJson,
            StatusCode = 200,
            ContentType = FhirConstants.FhirMediaType
        };
    }

    [HttpGet("ServiceRequest")]
    [SwaggerGetServiceRequest]
    public IActionResult GetServiceRequest(
       [FromQuery(Name = "patient.identifier")] string? patientIdentifier)
    {
        _logger.CalledMethod(nameof(GetServiceRequest));

        // I intentionally do not parse or validate the identifier,
        // because the endpoint is not implemented.
        throw new ProxyNotImplementedException(
            "BaRS did not recognize the request. This request has not been implemented within the Api.");
    }
}
