using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace NWRI.eReferralsService.API.Configuration;

[ExcludeFromCodeCoverage]
public class WpasApiConfig
{
    public static string SectionName => "WpasApi";

    [Required]
    public required string BaseUrl { get; set; }

    [Required]
    public required string CreateReferralEndpoint { get; set; }

    [Required]
    public required string CancelReferralEndpoint { get; set; }

    [Required]
    public required string GetReferralEndpoint { get; set; }

    [Required]
    public required int TimeoutSeconds { get; set; }
}
