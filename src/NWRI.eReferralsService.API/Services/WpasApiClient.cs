using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Extensions.Logger;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Models.WPAS.Responses;

namespace NWRI.eReferralsService.API.Services;

public sealed class WpasApiClient : IWpasApiClient
{
    private readonly HttpClient _httpClient;
    private readonly WpasApiConfig _wpasApiConfig;
    private readonly ILogger<WpasApiClient> _logger;

    public WpasApiClient(HttpClient httpClient, IOptions<WpasApiConfig> wpasApiOptions, ILogger<WpasApiClient> logger)
    {
        _httpClient = httpClient;
        _wpasApiConfig = wpasApiOptions.Value;
        _logger = logger;
    }

    public async Task<WpasCreateReferralResponse> CreateReferralAsync(WpasCreateReferralRequest request, CancellationToken cancellationToken)
    {
        return await PostAsync<WpasCreateReferralRequest, WpasCreateReferralResponse>(_wpasApiConfig.CreateReferralEndpoint, request, cancellationToken);
    }

    public async Task<WpasCancelReferralResponse> CancelReferralAsync(WpasCancelReferralRequest request, CancellationToken cancellationToken)
    {
        return await PostAsync<WpasCancelReferralRequest, WpasCancelReferralResponse>(_wpasApiConfig.CancelReferralEndpoint, request, cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest requestBody,
        CancellationToken cancellationToken)
        where TRequest : class
        where TResponse : WpasReferralResponse
    {
        var requestBodyJson = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(
            requestBodyJson,
            new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

        using var response = await _httpClient.PostAsync(
            endpoint,
            content,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var deserializedResponse = await JsonSerializer.DeserializeAsync<TResponse>(stream, cancellationToken: cancellationToken);
                return deserializedResponse ?? throw new JsonException($"Deserialization of response to {typeof(TResponse).Name} resulted in null.");
            }
            catch (JsonException exception)
            {
                throw new ProxyServerException(exception.Message);
            }
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
