using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NWRI.eReferralsService.API.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        HandleProcessMessage(operation, context);
        HandleGetReferral(operation, context);
        HandleGetReferrals(operation, context);
        HandleGetAppointments(operation, context);
        HandleGetAppointmentById(operation, context);
    }

    private static void HandleProcessMessage(OpenApiOperation operation, OperationFilterContext context)
    {
        var processMessageRequestAttribute = context.MethodInfo.GetCustomAttribute<SwaggerProcessMessageRequestAttribute>();

        if (processMessageRequestAttribute is null)
        {
            return;
        }

        operation.Parameters = [];

        AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddProcessMessageResponses(operation);
        AddProcessMessageRequests(operation);
    }

    private static void HandleGetReferral(OpenApiOperation operation, OperationFilterContext context)
    {
        var getReferralRequestAttribute = context.MethodInfo.GetCustomAttribute<SwaggerGetReferralRequestAttribute>();

        if (getReferralRequestAttribute is null)
        {
            return;
        }

        EnsureIdPathParameter(operation);

        AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddGetReferralResponses(operation);
    }

    private static void HandleGetReferrals(OpenApiOperation operation, OperationFilterContext context)
    {
        var attr = context.MethodInfo.GetCustomAttribute<SwaggerGetReferralsRequestAttribute>();
        if (attr is null)
        {
            return;
        }

        AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddGetReferralsResponses(operation);
    }

    private static void HandleGetAppointments(OpenApiOperation operation, OperationFilterContext context)
    {
        var attr = context.MethodInfo.GetCustomAttribute<SwaggerGetAppointmentsRequestAttribute>();
        if (attr is null)
        {
            return;
        }

        AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddGetAppointmentsResponses(operation);
    }
    private static void HandleGetAppointmentById(OpenApiOperation operation, OperationFilterContext context)
    {
        var getAppointmentByIdRequestAttribute = context.MethodInfo.GetCustomAttribute<SwaggerGetAppointmentByIdRequestAttribute>();

        if (getAppointmentByIdRequestAttribute is null)
        {
            return;
        }

        EnsureIdPathParameter(operation);

        AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddGetAppointmentByIdResponses(operation);
    }

    private static void AddHeaders(OpenApiOperation operation, IEnumerable<string> headers, bool isRequired)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        foreach (var header in headers)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                In = ParameterLocation.Header,
                Example = new OpenApiString(RequestHeaderKeys.GetExampleValue(header)),
                Required = isRequired,
                Name = header,
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }

    private static void AddProcessMessageRequests(OpenApiOperation operation)
    {
        operation.RequestBody = new OpenApiRequestBody();
        operation.RequestBody.Content.Add(FhirConstants.FhirMediaType,
            new OpenApiMediaType
            {
                Example = new OpenApiString(
                    File.ReadAllText("Swagger/Examples/process-message-payload-and-response.json"))
            });
    }

    private static void EnsureIdPathParameter(OpenApiOperation operation)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        var exists = operation.Parameters.Any(p =>
            p.In == ParameterLocation.Path &&
            string.Equals(p.Name, "id", StringComparison.OrdinalIgnoreCase));

        if (exists) return;

        operation.Parameters.Add(new OpenApiParameter
        {
            In = ParameterLocation.Path,
            Name = "id",
            Required = true,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" },
            Example = new OpenApiString(Guid.NewGuid().ToString())
        });
    }

    private static OpenApiResponse CreateFhirResponseWithExample(string description, string examplePath)
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

    private static void AddGetReferralResponses(OpenApiOperation operation)
    {
        operation.Responses = new OpenApiResponses
        {
            ["200"] = CreateFhirResponseWithExample(
                "OK",
                "Swagger/Examples/get-referral-ok-response.json"),
            ["400"] = CreateFhirResponseWithExample(
                "Bad Request",
                "Swagger/Examples/get-referral-bad-request.json"),
            ["404"] = CreateFhirResponseWithExample(
                "Not Found",
                "Swagger/Examples/get-referral-not-found.json"),
            ["429"] = CreateFhirResponseWithExample(
                "Too many requests",
                "Swagger/Examples/common-too-many-requests.json"),
            ["500"] = CreateFhirResponseWithExample(
                "Internal Server Error",
                "Swagger/Examples/common-internal-server-error.json"),
            ["503"] = CreateFhirResponseWithExample(
                "Service Unavailable",
                "Swagger/Examples/common-external-server-error.json")
        };
    }

    private static void AddProcessMessageResponses(OpenApiOperation operation)
    {
        operation.Responses = new OpenApiResponses
        {
            ["200"] = CreateFhirResponseWithExample(
                "OK",
                "Swagger/Examples/process-message-payload-and-response.json"),
            ["400"] = CreateFhirResponseWithExample(
                "Bad Request",
                "Swagger/Examples/process-message-bad-request.json"),
            ["429"] = CreateFhirResponseWithExample(
                "Too many requests",
                "Swagger/Examples/common-too-many-requests.json"),
            ["500"] = CreateFhirResponseWithExample(
                "Internal Server Error",
                "Swagger/Examples/common-internal-server-error.json"),
            ["503"] = CreateFhirResponseWithExample(
                "Service Unavailable",
                "Swagger/Examples/common-external-server-error.json")
        };
    }

    private static void AddGetReferralsResponses(OpenApiOperation operation)
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

    private static void AddGetAppointmentsResponses(OpenApiOperation operation)
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

    private static void AddGetAppointmentByIdResponses(OpenApiOperation operation)
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
