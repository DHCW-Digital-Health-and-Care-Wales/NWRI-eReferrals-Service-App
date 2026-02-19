using System.Diagnostics.CodeAnalysis;
using System.Net;
using Azure.Identity;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Configuration.Resilience;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.HealthChecks;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.API.Validators;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace NWRI.eReferralsService.API.Extensions;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static void AddApplicationInsights(this IServiceCollection services, bool isDevelopmentEnvironment, IConfiguration configuration)
    {
        var appInsightsConnectionString = configuration.GetRequiredSection(ApplicationInsightsConfig.SectionName)
            .GetValue<string>(nameof(ApplicationInsightsConfig.ConnectionString));

        services.AddApplicationInsightsTelemetry(options => options.ConnectionString = appInsightsConnectionString);
        services.AddSingleton<ITelemetryInitializer, EnrichLoggerContext>();
        services.Configure<TelemetryConfiguration>(config =>
        {
            if (isDevelopmentEnvironment)
            {
                config.SetAzureTokenCredential(new AzureCliCredential());
                return;
            }

            var clientId = configuration.GetRequiredSection(ManagedIdentityConfig.SectionName)
                .GetValue<string>(nameof(ManagedIdentityConfig.ClientId));
            config.SetAzureTokenCredential(new ManagedIdentityCredential(clientId));
        });
    }

    public static void AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<BundleCreateReferralModel>, BundleCreateReferralModelValidator>();
        services.AddScoped<IValidator<BundleCancelReferralModel>, BundleCancelReferralModelValidator>();
        services.AddScoped<IValidator<HeadersModel>, HeadersModelValidator>();
    }

    public static void AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<IWpasApiClient, WpasApiClient>((provider, client) =>
        {
            var wpasApiConfig = provider.GetRequiredService<IOptions<WpasApiConfig>>().Value;
            client.BaseAddress = new Uri(wpasApiConfig.BaseUrl);
        }).AddResilienceHandler("default", CreateResiliencePipeline);
    }

    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IReferralService, ReferralService>();
        services.AddScoped<IWpasOutpatientReferralMapper, WpasOutpatientReferralMapper>();
        services.AddSingleton<IWpasJsonSchemaValidator, WpasWpasJsonSchemaValidator>();
    }

    public static void AddCustomHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<ApplicationLivenessHealthCheck>("Application Liveness", tags: ["live"])
            .AddCheck<FhirBundleProfileValidatorHealthCheck>("FHIR Bundle Profile Validator", tags: ["ready"]);
    }

    private static void CreateResiliencePipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        ResilienceHandlerContext context)
    {
        var resilienceConfig = context.ServiceProvider.GetRequiredService<IOptions<ResilienceConfig>>().Value;

        builder
            .AddTimeout(TimeSpan.FromSeconds(resilienceConfig.TotalTimeoutSeconds))
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                BackoffType = resilienceConfig.Retry.IsExponentialDelay
                    ? DelayBackoffType.Exponential
                    : DelayBackoffType.Constant,
                Delay = TimeSpan.FromSeconds(resilienceConfig.Retry.DelaySeconds),
                UseJitter = true,
                MaxRetryAttempts = resilienceConfig.Retry.MaxRetries,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<TimeoutRejectedException>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => response.StatusCode
                        is HttpStatusCode.RequestTimeout
                        or HttpStatusCode.TooManyRequests
                        or HttpStatusCode.InternalServerError
                        or HttpStatusCode.BadGateway
                        or HttpStatusCode.ServiceUnavailable
                        or HttpStatusCode.GatewayTimeout)
            })
            .AddTimeout(TimeSpan.FromSeconds(resilienceConfig.AttemptTimeoutSeconds));
    }
}
