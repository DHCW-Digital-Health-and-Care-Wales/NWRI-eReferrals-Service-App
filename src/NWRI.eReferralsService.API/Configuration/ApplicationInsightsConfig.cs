using System.Diagnostics.CodeAnalysis;

namespace NWRI.eReferralsService.API.Configuration;

[ExcludeFromCodeCoverage]
public class ApplicationInsightsConfig
{
    public static string SectionName => "ApplicationInsights";

    public required string ConnectionString { get; set; }
}
