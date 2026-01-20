using Hl7.Fhir.Utility;
using WCCG.eReferralsService.API.Constants;

namespace WCCG.eReferralsService.API.Models;

public class HeadersModel
{
    //Required
    public required string? TargetIdentifier { get; set; }
    public required string? EndUserOrganisation { get; set; }
    public required string? RequestingSoftware { get; set; }
    public required string? RequestId { get; set; }
    public required string? CorrelationId { get; set; }
    public required string? UseContext { get; set; }
    public required string? Accept { get; set; }

    //Optional
    public required string? RequestingPractitioner { get; set; }

    public static HeadersModel FromHeaderDictionary(IHeaderDictionary headerDictionary)
    {
        return new HeadersModel
        {
            //Required
            TargetIdentifier = headerDictionary.GetOrDefault(RequestHeaderKeys.TargetIdentifier),
            EndUserOrganisation = headerDictionary.GetOrDefault(RequestHeaderKeys.EndUserOrganisation),
            RequestingSoftware = headerDictionary.GetOrDefault(RequestHeaderKeys.RequestingSoftware),
            RequestId = headerDictionary.GetOrDefault(RequestHeaderKeys.RequestId),
            CorrelationId = headerDictionary.GetOrDefault(RequestHeaderKeys.CorrelationId),
            UseContext = headerDictionary.GetOrDefault(RequestHeaderKeys.UseContext),
            Accept = headerDictionary.GetOrDefault(RequestHeaderKeys.Accept),

            //Optional
            RequestingPractitioner = headerDictionary.GetOrDefault(RequestHeaderKeys.RequestingPractitioner),
        };
    }
}
