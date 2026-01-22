using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Extensions;

namespace NWRI.eReferralsService.API.Models;

public sealed class BundleCancelReferralModel : IBundleModel<BundleCancelReferralModel>
{
    public required MessageHeader? MessageHeader { get; set; }
    public required ServiceRequest? ServiceRequest { get; set; }
    public required Patient? Patient { get; set; }
    public required List<Organization>? Organizations { get; set; }

    public static BundleCancelReferralModel FromBundle(Bundle bundle)
    {
        return new BundleCancelReferralModel
        {
            MessageHeader = bundle.ResourceByType<MessageHeader>(),
            ServiceRequest = bundle.ResourceByType<ServiceRequest>(),
            Patient = bundle.ResourceByType<Patient>(),
            Organizations = bundle.ResourcesByType<Organization>().ToList()
        };
    }
}