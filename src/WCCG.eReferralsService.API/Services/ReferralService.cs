using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentValidation;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WCCG.eReferralsService.API.Configuration;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Exceptions;
using WCCG.eReferralsService.API.Extensions;
using WCCG.eReferralsService.API.Models;
using WCCG.eReferralsService.API.Validators;
using Task = System.Threading.Tasks.Task;

namespace WCCG.eReferralsService.API.Services;

public class ReferralService : IReferralService
{
    private readonly HttpClient _httpClient;
    private readonly IValidator<BundleModel> _bundleValidator;
    private readonly IFhirBundleProfileValidator _fhirBundleProfileValidator;
    private readonly IValidator<HeadersModel> _headerValidator;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly PasReferralsApiConfig _pasReferralsApiConfig;

    public ReferralService(HttpClient httpClient,
        IOptions<PasReferralsApiConfig> pasReferralsApiOptions,
        IValidator<BundleModel> bundleValidator,
        IFhirBundleProfileValidator fhirBundleProfileValidator,
        IValidator<HeadersModel> headerValidator,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _httpClient = httpClient;
        _bundleValidator = bundleValidator;
        _fhirBundleProfileValidator = fhirBundleProfileValidator;
        _headerValidator = headerValidator;
        _jsonSerializerOptions = jsonSerializerOptions;
        _pasReferralsApiConfig = pasReferralsApiOptions.Value;
    }

    public async Task<string> ProcessMessageAsync(IHeaderDictionary headers, string requestBody)
    {
        await ValidateHeaders(headers);

        var bundle = JsonSerializer.Deserialize<Bundle>(requestBody, _jsonSerializerOptions);
        if (bundle is null)
        {
            throw new JsonException("Request body deserialized to null Bundle");
        }

        var reasonCode = GetMessageReasonCode(bundle);
        return reasonCode switch
        {
            FhirConstants.BarsMessageReasonNew => await CreateReferralAsync(requestBody, bundle),
            FhirConstants.BarsMessageReasonDelete => await CancelReferralAsync(requestBody, bundle),
            null => throw new RequestParameterValidationException("MessageHeader.reason", "MessageHeader.reason.coding.code is required"),
            _ => throw new RequestParameterValidationException("MessageHeader.reason",
                $"Unsupported message reason '{reasonCode}'. Supported: '{FhirConstants.BarsMessageReasonNew}', '{FhirConstants.BarsMessageReasonDelete}'")
        };
    }

    public async Task<string> GetReferralAsync(IHeaderDictionary headers, string? id)
    {
        if (!Guid.TryParse(id, out _))
        {
            throw new RequestParameterValidationException(nameof(id), "Id should be a valid GUID");
        }

        await ValidateHeaders(headers);

        var endpoint = string.Format(CultureInfo.InvariantCulture, _pasReferralsApiConfig.GetReferralEndpoint, id);
        using var response = await _httpClient.GetAsync(endpoint);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        throw await GetNotSuccessfulApiCallException(response);
    }

    private static async Task<Exception> GetNotSuccessfulApiCallException(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(content);
            return new NotSuccessfulApiCallException(response.StatusCode, problemDetails!);
        }
        catch (JsonException)
        {
            return new NotSuccessfulApiCallException(response.StatusCode, content);
        }
    }

    private async Task ValidateHeaders(IHeaderDictionary headers)
    {
        var headersModel = HeadersModel.FromHeaderDictionary(headers);

        var headersValidationResult = await _headerValidator.ValidateAsync(headersModel);
        if (!headersValidationResult.IsValid)
        {
            // TODO: Add audit log HeadersValidationFailed
            throw new HeaderValidationException(headersValidationResult.Errors);
        }

        // TODO: Add audit log HeadersValidationSucceeded
    }

    private Task ValidateFhirProfile(Bundle bundle)
    {
        var validationOutput = _fhirBundleProfileValidator.Validate(bundle);
        if (!validationOutput.IsSuccessful)
        {
            // TODO: Add audit log FhirProfileValidationFailed
            throw new FhirProfileValidationException(validationOutput.Errors!);
        }

        // TODO: Add audit log FhirProfileValidationSucceeded
        return Task.CompletedTask;
    }

    private async Task ValidateMandatoryData(Bundle bundle)
    {
        var bundleModel = BundleModel.FromBundle(bundle);

        var bundleValidationResult = await _bundleValidator.ValidateAsync(bundleModel);
        if (!bundleValidationResult.IsValid)
        {
            // TODO: Add audit log MandatoryDataValidationFailed
            throw new BundleValidationException(bundleValidationResult.Errors);
        }

        // TODO: Add audit log MandatoryDataValidationSucceeded
    }

    private static string? GetMessageReasonCode(Bundle bundle)
    {
        var messageHeader = bundle.ResourceByType<MessageHeader>();
        return messageHeader?.Reason?.Coding
            .FirstOrDefault(c => string.Equals(c.System, FhirConstants.BarsMessageReasonSystem, StringComparison.Ordinal))
            ?.Code;
    }

    private async Task<string> CreateReferralAsync(string requestBody, Bundle bundle)
    {
        await ValidateFhirProfile(bundle);
        await ValidateMandatoryData(bundle);

        using var response = await _httpClient.PostAsync(_pasReferralsApiConfig.CreateReferralEndpoint,
            new StringContent(requestBody, new MediaTypeHeaderValue(FhirConstants.FhirMediaType)));

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        throw await GetNotSuccessfulApiCallException(response);
    }

    private Task<string> CancelReferralAsync(string requestBody, Bundle bundle)
    {
        // TODO: Implement cancel referral flow
        throw new NotImplementedException("CancelReferralAsync is not implemented yet");
    }
}
