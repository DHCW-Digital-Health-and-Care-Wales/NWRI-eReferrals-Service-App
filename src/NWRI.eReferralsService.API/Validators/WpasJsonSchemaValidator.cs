using System.Collections.Concurrent;
using System.Text.Json;
using Json.Schema;
using NWRI.eReferralsService.API.Models.WPAS.Requests;

namespace NWRI.eReferralsService.API.Validators;

public class WpasJsonSchemaValidator
{
    private const string WpasCreateReferralRequestJsonSchemaPath = "Schemas/WPAS-create-referral-request.schema.json";

    private readonly ConcurrentDictionary<string, JsonSchema> _schemaCache = new();
    private readonly IHostEnvironment _hostEnvironment;

    public WpasJsonSchemaValidator(IHostEnvironment hostEnvironment)
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

        var schema = _schemaCache.GetOrAdd(schemaPath, static path => JsonSchema.FromFile(path));

        return schema.Evaluate(jsonElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
    }
}
