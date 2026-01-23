using System.Text.Json;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Extensions;

namespace NWRI.eReferralsService.API.Helpers
{
    public static class Base64Decoder
    {
        private static readonly JsonSerializerOptions DefaultFhirJsonSerializerOptions =
            new JsonSerializerOptions().ForFhirExtended();

        public static bool TryDecode<T>(string? base64Value, out T? result)
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
                result = JsonSerializer.Deserialize<T>(bytes, DefaultFhirJsonSerializerOptions);
                return result is not null;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
