using System.Text.Json;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Extensions.Logger;

namespace NWRI.eReferralsService.API.Helpers;

public class FhirBase64Decoder
{
    private readonly ILogger<FhirBase64Decoder> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public FhirBase64Decoder(ILogger<FhirBase64Decoder> logger, JsonSerializerOptions serializerOptions)
    {
        _logger = logger;
        _serializerOptions = serializerOptions;
    }

    public bool TryDecode<T>(string? base64Value, out T? result)
        where T : Base
    {
        result = null;

        if (string.IsNullOrWhiteSpace(base64Value))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(base64Value.Trim());
            result = JsonSerializer.Deserialize<T>(bytes, _serializerOptions);
            return result != null;
        }
        catch (Exception exception)
        {
            _logger.Base64DecodingFailure(exception);
            return false;
        }
    }
}