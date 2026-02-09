using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.API.Services;

public sealed class WpasApiClient : IWpasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WpasApiConfig _wpasApiConfig;
    private readonly IEventLogger _eventLogger;

    public WpasApiClient(HttpClient httpClient, IOptions<WpasApiConfig> wpasApiOptions, IEventLogger eventLogger)
    {
        _httpClient = httpClient;
        _wpasApiConfig = wpasApiOptions.Value;
        _eventLogger = eventLogger;
    }

    public Task<string> CreateReferralAsync(string requestBody, CancellationToken cancellationToken)
    {
        return PostAsync(_wpasApiConfig.CreateReferralEndpoint, requestBody, cancellationToken);
    }

    public Task<string> CancelReferralAsync(string requestBody, CancellationToken cancellationToken)
    {
        return PostAsync(_wpasApiConfig.CancelReferralEndpoint, requestBody, cancellationToken);
    }

    private async Task<string> PostAsync(string endpoint, string requestBody, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsync(
            endpoint,
            new StringContent(requestBody, new MediaTypeHeaderValue(FhirConstants.FhirMediaType)),
            cancellationToken);
        stopwatch.Stop();

        if (response.IsSuccessStatusCode)
        {
            // TODO: Extract WPAS referral ID from the response and pass it to the event
            _eventLogger.Audit(new EventCatalogue.DataSuccessfullyCommittedToWpas(stopwatch.ElapsedMilliseconds, null));

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        throw await GetNotSuccessfulApiCallExceptionAsync(response);
    }

    private static async Task<Exception> GetNotSuccessfulApiCallExceptionAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content);
            if (problemDetails != null)
            {
                return new NotSuccessfulApiCallException(response.StatusCode, problemDetails);
            }
        }
        catch (JsonException)
        {
            // Ignore deserialization errors and fallback to plain content
        }

        return new NotSuccessfulApiCallException(response.StatusCode, content);
    }
}
