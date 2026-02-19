using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
public sealed class BookingsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<SwaggerGetAppointmentsRequestAttribute>() is not null)
        {
            ApplyGetAppointments(operation);
        }
        else if (context.MethodInfo.GetCustomAttribute<SwaggerGetBookingSlotRequestAttribute>() is not null)
        {
            ApplyGetBookingSlot(operation);
        }
    }

    private static void ApplyGetAppointments(OpenApiOperation operation)
    {
        SwaggerHelpers.AddCommonHeaders(operation);
        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }

    private static void ApplyGetBookingSlot(OpenApiOperation operation)
    {
        SwaggerHelpers.AddCommonHeaders(operation);

        AddBookingSlotQueryParameters(operation);

        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }

    private static void AddBookingSlotQueryParameters(OpenApiOperation operation)
    {
        SwaggerHelpers.UpsertParameter(operation, new OpenApiParameter
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
        });

        SwaggerHelpers.UpsertParameter(operation, new OpenApiParameter
        {
            Name = "start",
            In = ParameterLocation.Query,
            Required = true,
            Description = "Use twice with ge and le prefixes to define time window.",
            Style = ParameterStyle.Form,
            Explode = true,
            Schema = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema { Type = "string" },
                Example = new OpenApiArray
                {
                    new OpenApiString("ge2022-03-01T12:00:00+00:00"),
                    new OpenApiString("le2022-03-01T13:30:00+00:00")
                }
            }
        });

        SwaggerHelpers.UpsertParameter(operation, new OpenApiParameter
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
        });
    }
}
