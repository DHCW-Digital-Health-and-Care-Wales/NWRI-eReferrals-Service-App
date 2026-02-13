using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NWRI.eReferralsService.API.Constants;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
internal static class SwaggerHelpers
{
    public static void EnsureParameters(OpenApiOperation operation)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
    }

    public static void AddHeaders(OpenApiOperation operation, IEnumerable<string> headers, bool isRequired)
    {
        EnsureParameters(operation);

        foreach (var header in headers)
        {
            AddParameterIfMissing(operation, new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Name = header,
                Required = isRequired,
                Example = new OpenApiString(RequestHeaderKeys.GetExampleValue(header)),
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }

    public static void AddPathParameter(OpenApiOperation operation, string name, bool required, IOpenApiAny? example = null)
    {
        EnsureParameters(operation);

        AddParameterIfMissing(operation, new OpenApiParameter
        {
            In = ParameterLocation.Path,
            Name = name,
            Required = required,
            Example = example
        });
    }

    public static void AddParameterIfMissing(OpenApiOperation operation, OpenApiParameter parameter)
    {
        EnsureParameters(operation);

        if (operation.Parameters.Any(p =>
                p.In == parameter.In &&
                p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(parameter);
    }

    public static OpenApiResponse CreateFhirResponseWithExample(string description, string examplePath)
    {
        return new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                {
                    RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept),
                    new OpenApiMediaType
                    {
                        Example = new OpenApiString(File.ReadAllText(examplePath))
                    }
                }
            }
        };
    }

    public static void AddProxyNotImplementedResponses(OpenApiOperation operation)
    {
        operation.Responses = new OpenApiResponses
        {
            ["429"] = CreateFhirResponseWithExample(
                "Too many requests",
                "Swagger/Examples/common-too-many-requests.json"),
            ["500"] = CreateFhirResponseWithExample(
                "Internal Server Error",
                "Swagger/Examples/common-internal-server-error.json"),
            ["501"] = CreateFhirResponseWithExample(
                "Not Implemented",
                "Swagger/Examples/common-proxy-not-implemented.json")
        };
    }
}
