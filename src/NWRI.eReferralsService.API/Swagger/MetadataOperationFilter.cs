using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Models;
using NWRI.eReferralsService.API.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
public sealed class MetadataOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<SwaggerGetMetadataRequestAttribute>() is not null)
        {
            SwaggerHelpers.AddHeaders(
                operation,
                RequestHeaderKeys.GetAllExcept(
                    RequestHeaderKeys.TargetIdentifier,
                    RequestHeaderKeys.RequestingPractitioner,
                    RequestHeaderKeys.UseContext),
                true);

            SwaggerHelpers.AddHeaders(operation, [RequestHeaderKeys.TargetIdentifier, RequestHeaderKeys.RequestingPractitioner], false);

            AddResponses(operation);
        }
    }

    private static void AddResponses(OpenApiOperation operation)
    {
        operation.Responses = new OpenApiResponses
        {
            ["200"] = SwaggerHelpers.CreateFhirResponseWithExample(
                "OK",
                "Resources/Fhir/metadata-capability-statement-response.json"),
            ["429"] = SwaggerHelpers.CreateFhirResponseWithExample(
                "Too many requests",
                "Swagger/Examples/common-too-many-requests.json"),
            ["500"] = SwaggerHelpers.CreateFhirResponseWithExample(
                "Internal Server Error",
                "Swagger/Examples/common-internal-server-error.json"),
            ["503"] = SwaggerHelpers.CreateFhirResponseWithExample(
                "Service Unavailable",
                "Swagger/Examples/common-external-server-error.json")
        };
    }
}
