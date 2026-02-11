using Microsoft.AspNetCore.Mvc;

namespace NWRI.eReferralsService.API.Queries
{
    public class GetBookingSlotQuery
    {
        [FromQuery(Name = "status")]
        public required string Status { get; set; }

        [FromQuery(Name = "start")]
        public required string[] Start { get; set; }

        [FromQuery(Name = "_include")]
        public required string[] Include { get; set; }
    }
}
