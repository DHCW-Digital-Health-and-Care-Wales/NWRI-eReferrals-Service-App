using System.Text.Json;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Extensions.Logger;

namespace NWRI.eReferralsService.API.Helpers
{
    public class Base64Decoder
    {
        private readonly ILogger<Base64Decoder> _logger;

        public Base64Decoder(ILogger<Base64Decoder> logger)
        {
            _logger = logger;
        }

        private readonly JsonSerializerOptions _serializerOptions =
            new JsonSerializerOptions().ForFhirExtended();

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
}
