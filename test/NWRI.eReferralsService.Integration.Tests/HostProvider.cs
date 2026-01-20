using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WCCG.eReferralsService.Integration.Tests;

public static class HostProvider
{
    public const string TestEndpoint = "/test";

    public static IHost StartHostWithEndpoint(
        RequestDelegate requestDelegate,
        Action<IServiceCollection>? addServices = null,
        Action<IApplicationBuilder>? configureApp = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddApplicationInsightsTelemetry(options => options.ConnectionString = "test");
                        addServices?.Invoke(services);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        configureApp?.Invoke(app);

                        app.UseEndpoints(endpoints => { endpoints.MapGet(TestEndpoint, requestDelegate); });
                    })
                    ;
            }).Start();
    }
}
