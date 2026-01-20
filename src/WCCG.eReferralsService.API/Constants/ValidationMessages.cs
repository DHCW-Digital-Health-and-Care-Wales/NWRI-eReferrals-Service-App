namespace WCCG.eReferralsService.API.Constants;

public static class ValidationMessages
{
    public static string InvalidFhirObject(string headerName, string typeName)
    {
        return $"Header '{headerName}' is not a valid '{typeName}' encoded FHIR object";
    }

    public static string NotGuidFormat(string headerName)
    {
        return $"Header '{headerName}' is not a valid GUID";
    }

    public static string NotExpectedFormat(string headerName, string expected)
    {
        return $"Header '{headerName}' has wrong format. Expected: {expected}";
    }

    public static string MissingRequiredHeader(string headerName)
    {
        return $"Missing required header '{headerName}'";
    }

    public static string MissingBundleEntity(string resourceName)
    {
        return $"The required FHIR bundle entity '{resourceName}' is missing";
    }

    public static string MissingEntityField<TResource>(string propertyName)
    {
        return $"{typeof(TResource).Name}.{propertyName} is required";
    }
}
