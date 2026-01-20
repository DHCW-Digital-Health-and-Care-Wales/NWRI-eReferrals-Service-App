using Hl7.Fhir.Model;

namespace WCCG.eReferralsService.API.Models
{
    public class BundleRequestCancelModel
    {
        public MessageHeader? MessageHeader { get; set; }
        public ServiceRequest? ServiceRequestRequest { get; set; }
        public ServiceRequest? ServiceRequestReferral { get; set; }
        public Patient? Patient { get; set; }
        public Organization? SendingOrganization { get; set; }   // or list + pick
        public List<Organization> Organizations { get; set; } = new();

        
    }

}
