using System.Diagnostics.CodeAnalysis;

namespace WCCG.eReferralsService.API.Configuration;

[ExcludeFromCodeCoverage]
public class ApplicationInsightsConfig
{
    public static string SectionName => "ApplicationInsights";

    public required string ConnectionString { get; set; }
}
