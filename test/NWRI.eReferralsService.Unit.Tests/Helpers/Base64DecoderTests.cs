using System.Text;
using System.Text.Json;
using FluentAssertions;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Helpers;

namespace NWRI.eReferralsService.Unit.Tests.Helpers;

public class Base64DecoderTests
{
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions().ForFhirExtended();
    private readonly Base64Decoder _decoder = new(NullLogger<Base64Decoder>.Instance);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void TryDecodeShouldReturnFalseWhenBase64ValueMissing(string? base64Value)
    {
        //Act
        var success = _decoder.TryDecode<Identifier>(base64Value, out var result);

        //Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryDecodeShouldReturnFalseWhenBase64Invalid()
    {
        //Act
        var success = _decoder.TryDecode<Identifier>("not-base64", out var result);

        //Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryDecodeShouldReturnFalseWhenDecodedJsonIsNull()
    {
        //Arrange
        var base64 = Convert.ToBase64String("null"u8.ToArray());

        //Act
        var success = _decoder.TryDecode<Identifier>(base64, out var result);

        //Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryDecodeShouldReturnFalseWhenDecodedJsonInvalid()
    {
        //Arrange
        var base64 = Convert.ToBase64String("{"u8.ToArray());

        //Act
        var success = _decoder.TryDecode<Identifier>(base64, out var result);

        //Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryDecodeShouldReturnTrueAndPopulateResultWhenValidBase64FhirJson()
    {
        //Arrange
        var expected = new Identifier("https://www.nhs.wales", "ABC-123");
        var base64 = ToBase64Json(expected);

        //Act
        var success = _decoder.TryDecode<Identifier>(base64, out var result);

        //Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.System.Should().Be(expected.System);
        result.Value.Should().Be(expected.Value);
    }

    [Fact]
    public void TryDecodeShouldTrimInput()
    {
        //Arrange
        var expected = new Identifier("https://www.nhs.wales", "ABC-123");
        var base64 = $"  {ToBase64Json(expected)}  ";

        //Act
        var success = _decoder.TryDecode<Identifier>(base64, out var result);

        //Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result.System.Should().Be(expected.System);
        result.Value.Should().Be(expected.Value);
    }

    private string ToBase64Json<T>(T value)
        where T : Base
    {
        var json = JsonSerializer.Serialize(value, _serializerOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}
