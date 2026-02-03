using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Helpers;

namespace NWRI.eReferralsService.API.Services;

public class HeadersDecoder : IHeadersDecoder
{
    private readonly Base64Decoder _base64Decoder;

    public HeadersDecoder(Base64Decoder base64Decoder)
    {
        _base64Decoder = base64Decoder;
    }

    public string? GetDecodedSourceSystem(string? requestingSoftwareHeader)
    {
        if (_base64Decoder.TryDecode<Device>(requestingSoftwareHeader, out var device) && device is not null)
        {
            return device.Identifier.FirstOrDefault()?.Value
                   ?? device.DeviceName.FirstOrDefault()?.Name;
        }

        return null;
    }

    public string? GetDecodedUserRole(string? requestingPractitionerHeader)
    {
        if (_base64Decoder.TryDecode<PractitionerRole>(requestingPractitionerHeader, out var practitionerRole)
            && practitionerRole is not null)
        {
            var coding = practitionerRole.Code.FirstOrDefault()?.Coding.FirstOrDefault();
            return coding?.Display ?? coding?.Code;
        }

        return null;
    }
}
