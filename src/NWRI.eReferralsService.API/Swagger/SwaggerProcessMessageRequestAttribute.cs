using System.Diagnostics.CodeAnalysis;

namespace NWRI.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public class SwaggerProcessMessageRequestAttribute : Attribute;
