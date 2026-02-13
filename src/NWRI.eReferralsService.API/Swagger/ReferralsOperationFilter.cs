using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NWRI.eReferralsService.API.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
public sealed class ReferralsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        HandleGetReferral(operation, context);
        HandleGetReferrals(operation, context);
    }

    private static void HandleGetReferral(OpenApiOperation operation, OperationFilterContext context)
    {
        var attr = context.MethodInfo.GetCustomAttribute<SwaggerGetReferralRequestAttribute>();
        if (attr is null) return;

        SwaggerHelpers.AddPathParameter(operation, "id", required: true, example: new OpenApiString(Guid.NewGuid().ToString()));

        SwaggerHelpers.AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        SwaggerHelpers.AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddGetReferralResponses(operation);
    }

    private static void HandleGetReferrals(OpenApiOperation operation, OperationFilterContext context)
    {
        var attr = context.MethodInfo.GetCustomAttribute<SwaggerGetReferralsRequestAttribute>();
        if (attr is null) return;

        SwaggerHelpers.AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        SwaggerHelpers.AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);

        AddGetReferralsResponses(operation);
    }

    private static void AddGetReferralResponses(OpenApiOperation operation)
    {
        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }

    private static void AddGetReferralsResponses(OpenApiOperation operation)
    {
        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }
}
