using System.Text;
using System.Text.Json;
using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Helpers;
using NWRI.eReferralsService.API.Services;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public class RequestFhirHeadersDecoderTests
{
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions().ForFhirExtended();
    private readonly RequestFhirHeadersDecoder _sut = new(
        new FhirBase64Decoder(
            NullLogger<FhirBase64Decoder>.Instance,
            new JsonSerializerOptions().ForFhirExtended()));

    [Fact]
    public void GetDecodedSourceSystemShouldReturnIdentifierValueWhenPresent()
    {
        var device = new Device();
        device.Identifier.Add(new Identifier { Value = "SYS-123" });

        var result = _sut.GetDecodedSourceSystem(ToBase64Json(device));

        result.Should().Be("SYS-123");
    }

    [Fact]
    public void GetDecodedSourceSystemShouldFallbackToDeviceNameWhenIdentifierMissing()
    {
        var device = new Device();
        device.DeviceName.Add(new Device.DeviceNameComponent
        {
            Name = "Some App",
            Type = DeviceNameType.UserFriendlyName
        });

        var result = _sut.GetDecodedSourceSystem(ToBase64Json(device));

        result.Should().Be("Some App");
    }

    [Fact]
    public void GetDecodedUserRoleShouldReturnDisplayWhenPresent()
    {
        var practitionerRole = new PractitionerRole();
        practitionerRole.Code.Add(new CodeableConcept
        {
            Coding =
            [
                new Coding { Display = "GP", Code = "GPCODE" }
            ]
        });

        var result = _sut.GetDecodedUserRole(ToBase64Json(practitionerRole));

        result.Should().Be("GP");
    }

    [Fact]
    public void GetDecodedUserRoleShouldFallbackToCodeWhenDisplayMissing()
    {
        var practitionerRole = new PractitionerRole();
        practitionerRole.Code.Add(new CodeableConcept
        {
            Coding =
            [
                new Coding { Code = "GPCODE" }
            ]
        });

        var result = _sut.GetDecodedUserRole(ToBase64Json(practitionerRole));

        result.Should().Be("GPCODE");
    }

    private string ToBase64Json<T>(T value)
        where T : Base
    {
        var json = JsonSerializer.Serialize(value, _serializerOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}