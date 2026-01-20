using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace WCCG.eReferralsService.API.Configuration.Resilience;

[ExcludeFromCodeCoverage]
public class ResilienceConfig
{
    public static string SectionName => "Resilience";

    [ValidateObjectMembers]
    public required RetryConfig Retry { get; set; }

    [Required]
    [Range(0, 60)]
    public required int TotalTimeoutSeconds { get; set; }

    [Required]
    [Range(0, 60)]
    public required int AttemptTimeoutSeconds { get; set; }
}
