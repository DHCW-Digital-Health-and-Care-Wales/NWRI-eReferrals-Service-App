using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
public sealed class ReferralsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.GetCustomAttribute<SwaggerGetReferralByIdRequestAttribute>() is not null)
        {
            ApplyGetReferral(operation);
        }
        else if (context.MethodInfo.GetCustomAttribute<SwaggerGetReferralsRequestAttribute>() is not null)
        {
            ApplyGetReferrals(operation);
        }
    }

    private static void ApplyGetReferral(OpenApiOperation operation)
    {
        SwaggerHelpers.AddCommonHeaders(operation);
        SwaggerHelpers.AddPathParameter(operation, "id", required: true, example: new OpenApiString(Guid.NewGuid().ToString()));
        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }

    private static void ApplyGetReferrals(OpenApiOperation operation)
    {
        SwaggerHelpers.AddCommonHeaders(operation);
        SwaggerHelpers.AddProxyNotImplementedResponses(operation);
    }
}
