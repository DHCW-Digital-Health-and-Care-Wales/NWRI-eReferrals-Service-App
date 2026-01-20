using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using WCCG.eReferralsService.API.Constants;

namespace WCCG.eReferralsService.API.Swagger;

[ExcludeFromCodeCoverage]
public class SwaggerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        HandleProcessMessage(operation, context);
        HandleGetReferral(operation, context);
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
                    File.ReadAllText("Swagger/Examples/process-message-payload&response.json"))
            });
    }

    private static void AddGetReferralResponses(OpenApiOperation operation)
    {
        operation.Responses = new OpenApiResponses
        {
            {
                "200", new OpenApiResponse
                {
                    Description = "OK",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(
                                    File.ReadAllText("Swagger/Examples/get-referral-ok-response.json")),
                            }
                        }
                    }
                }
            },
            {
                "400", new OpenApiResponse
                {
                    Description = "Bad Request",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(File.ReadAllText("Swagger/Examples/get-referral-bad-request.json")),
                            }
                        }
                    }
                }
            },
            {
                "404", new OpenApiResponse
                {
                    Description = "Not Found",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(File.ReadAllText("Swagger/Examples/get-referral-not-found.json")),
                            }
                        }
                    }
                }
            },
            {
                "429", new OpenApiResponse
                {
                    Description = "Too many requests",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(File.ReadAllText("Swagger/Examples/common-too-many-requests.json")),
                            }
                        }
                    }
                }
            },
            {
                "500", new OpenApiResponse
                {
                    Description = "Internal Server Error",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(
                                    File.ReadAllText("Swagger/Examples/common-internal-server-error.json")),
                            }
                        }
                    }
                }
            },
            {
                "503", new OpenApiResponse
                {
                    Description = "Service Unavailable",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(
                                    File.ReadAllText("Swagger/Examples/common-external-server-error.json")),
                            }
                        }
                    }
                }
            }
        };
    }

    private static void AddProcessMessageResponses(OpenApiOperation operation)
    {
        operation.Responses = new OpenApiResponses
        {
            {
                "200", new OpenApiResponse
                {
                    Description = "OK",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(
                                    File.ReadAllText("Swagger/Examples/process-message-payload&response.json")),
                            }
                        }
                    }
                }
            },
            {
                "400", new OpenApiResponse
                {
                    Description = "Bad Request",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(File.ReadAllText("Swagger/Examples/process-message-bad-request.json")),
                            }
                        }
                    }
                }
            },
            {
                "429", new OpenApiResponse
                {
                    Description = "Too many requests",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(File.ReadAllText("Swagger/Examples/common-too-many-requests.json")),
                            }
                        }
                    }
                }
            },
            {
                "500", new OpenApiResponse
                {
                    Description = "Internal Server Error",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(
                                    File.ReadAllText("Swagger/Examples/common-internal-server-error.json")),
                            }
                        }
                    }
                }
            },
            {
                "503", new OpenApiResponse
                {
                    Description = "Service Unavailable",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        {
                            RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept), new OpenApiMediaType
                            {
                                Example = new OpenApiString(
                                    File.ReadAllText("Swagger/Examples/common-external-server-error.json")),
                            }
                        }
                    }
                }
            }
        };
    }
}
