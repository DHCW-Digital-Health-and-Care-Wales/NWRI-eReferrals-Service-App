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
        if (context.MethodInfo.GetCustomAttribute<SwaggerGetReferralRequestAttribute>() is not null)
        {
            ApplyGetReferral(operation);
            return;
        }

        if (context.MethodInfo.GetCustomAttribute<SwaggerGetReferralsRequestAttribute>() is not null)
        {
            ApplyGetReferrals(operation);
        }
    }

    private static void ApplyGetReferral(OpenApiOperation operation)
    {
        AddCommonHeaders(operation);

        SwaggerHelpers.AddPathParameter(operation, "id", required: true, example: new OpenApiString(Guid.NewGuid().ToString()));
        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }

    private static void ApplyGetReferrals(OpenApiOperation operation)
    {
        AddCommonHeaders(operation);

        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }

    private static void AddCommonHeaders(OpenApiOperation operation)
    {
        SwaggerHelpers.AddHeaders(operation, RequestHeaderKeys.GetAllRequired(), true);
        SwaggerHelpers.AddHeaders(operation, RequestHeaderKeys.GetAllOptional(), false);
    }
}
