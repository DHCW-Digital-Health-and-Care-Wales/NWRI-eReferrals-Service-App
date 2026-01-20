using Hl7.Fhir.Model;

namespace WCCG.eReferralsService.API.Extensions;

public static class FhirSearchExtensions
{
    public static T? ResourceByType<T>(this Bundle bundle, string? id = null) where T : Resource
    {
        var resultList = bundle.Entry?.Select(e => e.Resource).OfType<T>() ?? [];
        return id is not null
            ? resultList.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
            : resultList.FirstOrDefault();
    }

    public static IEnumerable<T> ResourcesByType<T>(this Bundle bundle) where T : Resource
    {
        return bundle.Entry?.Select(e => e.Resource).OfType<T>() ?? [];
    }

    public static IEnumerable<T> ResourcesByProfile<T>(this Bundle bundle, string profile) where T : Resource
    {
        if (bundle.Entry == null) return [];
        return bundle.Entry
            .Select(e => e.Resource)
            .OfType<T>()
            .Where(r => r.HasProfile(profile));
    }

    public static IEnumerable<T> ResourcesExcludingProfile<T>(this Bundle bundle, string profile) where T : Resource
    {
        if (bundle.Entry == null) return [];
        return bundle.Entry
            .Select(e => e.Resource)
            .OfType<T>()
            .Where(r => !r.HasProfile(profile));
    }

    private static bool HasProfile(this Resource r, string profile)
    {
        var profiles = r.Meta?.Profile;
        return profiles != null && profiles.Any(p => p != null && p.Contains(profile, StringComparison.OrdinalIgnoreCase));
    }
}
