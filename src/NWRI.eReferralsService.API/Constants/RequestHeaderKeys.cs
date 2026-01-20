using System.Diagnostics.CodeAnalysis;

namespace WCCG.eReferralsService.API.Constants;

[ExcludeFromCodeCoverage]
public static class RequestHeaderKeys
{
    //Required
    public const string TargetIdentifier = "NHSD-Target-Identifier";
    public const string EndUserOrganisation = "NHSD-End-User-Organisation";
    public const string RequestingSoftware = "NHSD-Requesting-Software";
    public const string RequestId = "X-Request-Id";
    public const string CorrelationId = "X-Correlation-Id";
    public const string UseContext = "use-context";
    public const string Accept = "Accept";

    //Optional
    public const string RequestingPractitioner = "NHSD-Requesting-Practitioner";

    private static readonly Dictionary<string, string> HeaderExamplesDictionary = new()
    {
        { TargetIdentifier, "eyJzeXN0ZW0iOiJodHRwczovL2ZoaXIubmhzLnVrL0lkL2Rvcy1zZXJ2aWNlLWlkIiwidmFsdWUiOiIyMDAwMDExMTQ3In0=" },
        {
            EndUserOrganisation,
            "eyJyZXNvdXJjZVR5cGUiOiJPcmdhbml6YXRpb24iLCJpZGVudGlmaWVyIjpbeyJ2YWx1ZSI6IkExMDAxIiwic3lzdGVtIjoiaHR0cHM6Ly9maGlyLm5ocy51ay9JZC9vZHMtb3JnYW5pemF0aW9uLWNvZGUifV0sIm5hbWUiOiJNeSBzZXJ2aWNlIHByb3ZpZGVyIG5hbWUifQo="
        },
        {
            RequestingSoftware,
            "eyJyZXNvdXJjZVR5cGUiOiJEZXZpY2UiLCJpZGVudGlmaWVyIjpbeyJzeXN0ZW0iOiJodHRwczovL2NvbnN1bWVyc3VwcGxpZXIuY29tL0lkL2RldmljZS1pZGVudGlmaWVyIiwidmFsdWUiOiJTVVBQLUFQUC0xIn1dLCJkZXZpY2VOYW1lIjpbeyJuYW1lIjoiU3VwcGxpZXIgcHJvZHVjdCBuYW1lIiwidHlwZSI6Im1hbnVmYWN0dXJlci1uYW1lIn1dLCJ2ZXJzaW9uIjpbeyJ2YWx1ZSI6IjEuMC4wIn1dfQ=="
        },
        { RequestId, "c1ab3fba-6bae-4ba4-b257-5a87c44d4a91" },
        { CorrelationId, "9562466f-c982-4bd5-bb0e-255e9f5e6689" },
        { UseContext, "a4t2|validation|servicerequest-response|new" },
        { Accept, "application/fhir+json; version=1.2.0" },
        {
            RequestingPractitioner,
            "eyJyZXNvdXJjZVR5cGUiOiJQcmFjdGl0aW9uZXJSb2xlIiwiaWRlbnRpZmllciI6W3sic3lzdGVtIjoiaHR0cHM6Ly9maGlyLm5ocy51ay9JZC9zZHMtcm9sZS1wcm9maWxlLWlkIiwidmFsdWUiOiIxMDAzMzQ5OTM1MTQifV0sImNvZGUiOlt7ImNvZGluZyI6W3sic3lzdGVtIjoiaHR0cDovL3Nub21lZC5pbmZvL3NjdCIsImNvZGUiOiI2MjI0NzAwMSIsImRpc3BsYXkiOiJHZW5lcmFsIHByYWN0aXRpb25lciJ9XX1dfQ=="
        }
    };

    public static IEnumerable<string> GetAllRequired()
    {
        return [TargetIdentifier, EndUserOrganisation, RequestingSoftware, RequestId, CorrelationId, UseContext, Accept];
    }

    public static IEnumerable<string> GetAllOptional()
    {
        return [RequestingPractitioner];
    }

    public static IEnumerable<string> GetAll()
    {
        return GetAllRequired().Concat(GetAllOptional());
    }

    public static string GetExampleValue(string headerName)
    {
        return HeaderExamplesDictionary.GetValueOrDefault(headerName, string.Empty);
    }
}
