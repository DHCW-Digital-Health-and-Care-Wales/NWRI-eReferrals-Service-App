using System.Collections.Concurrent;
using System.Text.Json;
using Json.Schema;
using NWRI.eReferralsService.API.Models.WPAS;

namespace NWRI.eReferralsService.API.Validators;

public sealed class WpasWpasJsonSchemaValidator : IWpasJsonSchemaValidator
{
    private const string WpasCreateReferralRequestJsonSchemaPath = "Schemas/WPAS-create-referral-request.schema.json";

    private static readonly ConcurrentDictionary<string, JsonSchema> SchemaCache = new();
    private readonly IHostEnvironment _hostEnvironment;

    public WpasWpasJsonSchemaValidator(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public EvaluationResults ValidateWpasCreateReferralRequest(WpasCreateReferralRequest wpasCreateReferralRequest)
    {
        return Validate(wpasCreateReferralRequest, WpasCreateReferralRequestJsonSchemaPath);
    }

    private EvaluationResults Validate<T>(T model, string jsonSchemaPath)
    {
        var jsonElement = JsonSerializer.SerializeToElement(model);
        var schemaPath = Path.Combine(_hostEnvironment.ContentRootPath, jsonSchemaPath);

        var schema = SchemaCache.GetOrAdd(schemaPath, static path => JsonSchema.FromFile(path));

        return schema.Evaluate(jsonElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
    }
}
