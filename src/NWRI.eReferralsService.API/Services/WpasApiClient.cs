using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Models.WPAS;

namespace NWRI.eReferralsService.API.Services;

public sealed class WpasApiClient : IWpasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WpasApiConfig _wpasApiConfig;
    private readonly IEventLogger _eventLogger;
    private readonly ILogger<WpasApiClient> _logger;

    public WpasApiClient(HttpClient httpClient, IOptions<WpasApiConfig> wpasApiOptions, IEventLogger eventLogger, ILogger<WpasApiClient> logger)
    {
        _httpClient = httpClient;
        _wpasApiConfig = wpasApiOptions.Value;
        _eventLogger = eventLogger;
        _logger = logger;
    }

    public Task<WpasCreateReferralResponse?> CreateReferralAsync(WpasCreateReferralRequest request, CancellationToken cancellationToken)
    {
        return PostAsync<WpasCreateReferralResponse>(_wpasApiConfig.CreateReferralEndpoint, request, cancellationToken);
    }

    public Task<WpasCancelReferralResponse?> CancelReferralAsync(WpasCancelReferralRequest request, CancellationToken cancellationToken)
    {
        return PostAsync<WpasCancelReferralResponse>(_wpasApiConfig.CancelReferralEndpoint, request, cancellationToken);
    }

    private async Task<TResponse?> PostAsync<TResponse>(
        string endpoint,
        object requestBody,
        CancellationToken cancellationToken)
        where TResponse : IWpasReferralResponse
    {
        var stopwatch = Stopwatch.StartNew();

        var requestBodyJson = JsonSerializer.Serialize(requestBody);
        using var response = await _httpClient.PostAsync(
            endpoint,
            new StringContent(requestBodyJson, new MediaTypeHeaderValue(MediaTypeNames.Application.Json)),
            cancellationToken);
        stopwatch.Stop();

        if (response.IsSuccessStatusCode)
        {
            string? referralId = null;
            TResponse? responseModel = default;

            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                responseModel = await JsonSerializer.DeserializeAsync<TResponse>(stream, cancellationToken: cancellationToken);
                referralId = responseModel?.ReferralId;
            }
            catch (JsonException exception)
            {
                _logger.WpasResponseDeserializationFailed(exception, typeof(TResponse).Name);
            }
            finally
            {
                _eventLogger.Audit(new EventCatalogue.DataSuccessfullyCommittedToWpas(
                    stopwatch.ElapsedMilliseconds,
                    referralId));
            }

            return responseModel;
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
