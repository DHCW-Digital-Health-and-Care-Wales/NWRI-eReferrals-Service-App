using System.Diagnostics.CodeAnalysis;

namespace WCCG.eReferralsService.API.Configuration;

[ExcludeFromCodeCoverage]
public class ManagedIdentityConfig
{
    public static string SectionName => "ManagedIdentity";

    public required string ClientId { get; set; }
}
