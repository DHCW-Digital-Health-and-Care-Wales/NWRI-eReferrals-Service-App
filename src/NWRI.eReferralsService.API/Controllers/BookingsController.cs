using Microsoft.AspNetCore.Mvc;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Middleware;
using NWRI.eReferralsService.API.Queries;
using NWRI.eReferralsService.API.Swagger;

namespace NWRI.eReferralsService.API.Controllers;

[ApiController]
[AuditLogRequest]
public class BookingsController : ControllerBase
{
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(ILogger<BookingsController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Appointment")]
    [SwaggerGetAppointmentsRequest]
    public IActionResult GetAppointments()
    {
        _logger.CalledMethod(nameof(GetAppointments));

        throw new ProxyNotImplementedException();
    }

    [HttpGet("Slot")]
    [SwaggerGetBookingSlotRequest]
    public IActionResult GetBookingSlot([FromQuery] GetBookingSlotQuery query)
    {
        _logger.CalledMethod(nameof(GetBookingSlot));

        throw new ProxyNotImplementedException();
    }

    [HttpGet("Appointment/{id}")]
    [SwaggerGetAppointmentByIdRequest]
    public IActionResult GetAppointmentById(string id)
    {
        _logger.CalledMethod(nameof(GetAppointmentById));

        throw new ProxyNotImplementedException();
    }
}
