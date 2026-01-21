using System.Diagnostics.CodeAnalysis;

namespace NWRI.eReferralsService.API.Configuration;

[ExcludeFromCodeCoverage]
public class ManagedIdentityConfig
{
    public static string SectionName => "ManagedIdentity";

    public required string ClientId { get; set; }
}


