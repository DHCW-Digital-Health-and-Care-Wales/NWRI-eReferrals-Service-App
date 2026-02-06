using System.Text.Json;

namespace NWRI.eReferralsService.API.Serialization;

public interface IFhirJsonSerializerOptions
{
    JsonSerializerOptions Value { get; }
}
