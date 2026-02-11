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
        HandleGetBookingSlot(operation, context);
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

        operation.Parameters =
        [
            new OpenApiParameter
            {
                In = ParameterLocation.Path,
                Name = "id",
                Required = true,
                Example = new OpenApiString(Guid.NewGuid().ToString())
            }
        ];

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

    private static void HandleGetBookingSlot(OpenApiOperation operation, OperationFilterContext context)
    {
        var attr = context.MethodInfo.GetCustomAttribute<SwaggerGetBookingSlotRequestAttribute>();
        if (attr is null)
        {
            return;
        }

        AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddGetBookingSlotQueryOperation(operation);
        AddGetBookingSlotResponses(operation);
    }

    private static void AddHeaders(OpenApiOperation operation, IEnumerable<string> headers, bool isRequired)
    {
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

    private static void AddGetBookingSlotQueryOperation(OpenApiOperation operation)
    {
        operation.Parameters =
        [
            new OpenApiParameter
            {
                Name = "status",
                In = ParameterLocation.Query,
                Required = true,
                Description = "Comma-separated Slot status values (free, busy). Default: free.",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Example = new OpenApiString("free,busy")
                }
            },
            new OpenApiParameter
            {
                Name = "start",
                In = ParameterLocation.Query,
                Required = true,
                Description = "Use twice with ge and le prefixes to define time window.",
                Schema = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema { Type = "string" },
                    Example = new OpenApiArray
                    {
                        new OpenApiString("ge2022-03-01T12:00:00+00:00"),
                        new OpenApiString("le2022-03-01T13:30:00+00:00")
                    }
                },
                Style = ParameterStyle.Form,
                Explode = true
            },
            new OpenApiParameter
            {
                Name = "_include",
                In = ParameterLocation.Query,
                Required = true,
                Description =
                    "FHIR _include parameters. Repeat the parameter to include multiple values. " +
                    "Minimum required: Slot:schedule and Schedule:actor:HealthcareService. " +
                    "Unsupported _include values will be ignored and omitted from the response Bundle.link.url.",
                Style = ParameterStyle.Form,
                Explode = true,
                Schema = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "string",
                        Enum = new List<IOpenApiAny>
                        {
                            new OpenApiString("Slot:schedule"),
                            new OpenApiString("Schedule:actor:Practitioner"),
                            new OpenApiString("Schedule:actor:PractitionerRole"),
                            new OpenApiString("Schedule:actor:HealthcareService"),
                            new OpenApiString("HealthcareService:providedBy"),
                            new OpenApiString("HealthcareService:location"),
                            new OpenApiString("Slot:*")
                        }
                    }
                },
                Example = new OpenApiArray
                {
                    new OpenApiString("Slot:schedule"),
                    new OpenApiString("Schedule:actor:HealthcareService")
                }
            }
        ];
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

    private static void AddGetBookingSlotResponses(OpenApiOperation operation)
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
