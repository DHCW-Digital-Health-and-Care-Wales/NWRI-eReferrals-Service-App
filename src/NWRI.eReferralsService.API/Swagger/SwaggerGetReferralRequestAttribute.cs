using System.Diagnostics.CodeAnalysis;

namespace WCCG.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public class SwaggerGetReferralRequestAttribute : Attribute;
