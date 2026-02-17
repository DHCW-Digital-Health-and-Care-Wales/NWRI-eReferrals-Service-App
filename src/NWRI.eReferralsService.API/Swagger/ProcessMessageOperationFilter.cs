using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NWRI.eReferralsService.API.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
public sealed class ProcessMessageOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<SwaggerProcessMessageRequestAttribute>() is not null)
        {
            operation.Parameters ??= new List<OpenApiParameter>();
            operation.Parameters.Clear();

            SwaggerHelpers.AddCommonHeaders(operation);

            AddRequestBody(operation);
            AddResponses(operation);
        }
    }

    private static void AddRequestBody(OpenApiOperation operation)
    {
        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                [FhirConstants.FhirMediaType] = new OpenApiMediaType
                {
                    Example = new OpenApiString(
                        File.ReadAllText("Swagger/Examples/process-message-payload-and-response.json"))
                }
            }
        };
    }

    private static void AddResponses(OpenApiOperation operation)
    {
        operation.Responses = new OpenApiResponses
        {
            ["200"] = SwaggerHelpers.CreateFhirResponseWithExample(
                "OK",
                "Swagger/Examples/process-message-payload-and-response.json"),
            ["400"] = SwaggerHelpers.CreateFhirResponseWithExample(
                "Bad Request",
                "Swagger/Examples/process-message-bad-request.json"),
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
