using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace WCCG.eReferralsService.API.Extensions;

[ExcludeFromCodeCoverage]
public static class JsonSerializerOptionsExtensions
{
    public static JsonSerializerOptions ForFhirExtended(this JsonSerializerOptions options)
    {
        return options.ForFhir(ModelInfo.ModelInspector)
            .Pretty();
    }
}
