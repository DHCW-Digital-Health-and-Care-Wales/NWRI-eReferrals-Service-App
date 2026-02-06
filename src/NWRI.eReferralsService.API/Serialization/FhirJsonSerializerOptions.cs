using System.Text.Json;
using NWRI.eReferralsService.API.Extensions;

namespace NWRI.eReferralsService.API.Serialization;

public sealed class FhirJsonSerializerOptions : IFhirJsonSerializerOptions
{
    public JsonSerializerOptions Value { get; } = new JsonSerializerOptions().ForFhirExtended();
}
