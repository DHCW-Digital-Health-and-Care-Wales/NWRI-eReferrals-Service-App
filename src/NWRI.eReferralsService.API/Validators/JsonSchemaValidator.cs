using System.Collections.Concurrent;
using System.Text.Json;
using Json.Schema;
using NWRI.eReferralsService.API.Models.WPAS;

namespace NWRI.eReferralsService.API.Validators;

public sealed class JsonSchemaValidator : IJsonSchemaValidator
{
    public const string WpasCreateReferralRequestJsonSchemaPath = "Schemas/WPAS-create-referral-request.schema.json";

    private static readonly ConcurrentDictionary<string, JsonSchema> SchemaCache = new();
    private readonly IHostEnvironment _hostEnvironment;

    public JsonSchemaValidator(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public EvaluationResults Validate(WpasCreateReferralRequest wpasCreateReferralRequest, string jsonSchemaPath)
    {
        var jsonElement = JsonSerializer.SerializeToElement(wpasCreateReferralRequest);
        var schemaPath = Path.Combine(_hostEnvironment.ContentRootPath, jsonSchemaPath);

        var schema = SchemaCache.GetOrAdd(schemaPath, static path => JsonSchema.FromFile(path));

        var results = schema.Evaluate(jsonElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        return results;
    }
}
