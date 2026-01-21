using Hl7.Fhir.Model;

namespace WCCG.eReferralsService.API.Models
{
    public interface IBundleModel<T>
    {
        static abstract T FromBundle(Bundle bundle);
    }
}
