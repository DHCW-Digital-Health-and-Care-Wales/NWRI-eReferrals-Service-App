using System.Text.Json;
using Microsoft.Extensions.Options;
using WCCG.eReferralsService.API.Configuration.OptionValidators;
using WCCG.eReferralsService.API.Extensions;
using WCCG.eReferralsService.API.Middleware;
using WCCG.eReferralsService.API.Swagger;
using WCCG.eReferralsService.API.Configuration;
using WCCG.eReferralsService.API.Configuration.Resilience;
using WCCG.eReferralsService.API.Validators;

var builder = WebApplication.CreateBuilder(args);

//PasReferralsApiConfig
builder.Services.AddOptions<PasReferralsApiConfig>().Bind(builder.Configuration.GetSection(PasReferralsApiConfig.SectionName));
builder.Services.AddSingleton<IValidateOptions<PasReferralsApiConfig>, ValidatePasReferralsApiConfigOptions>();

//Resilience
builder.Services.AddOptions<ResilienceConfig>().Bind(builder.Configuration.GetSection(ResilienceConfig.SectionName));
builder.Services.AddSingleton<IValidateOptions<ResilienceConfig>, ValidateResilienceConfigOptions>();

builder.Services.AddOptions<FhirBundleProfileValidationConfig>().Bind(builder.Configuration.GetSection(FhirBundleProfileValidationConfig.SectionName));
builder.Services.AddSingleton<IFhirBundleProfileValidator, FhirBundleProfileValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options => { options.OperationFilter<SwaggerOperationFilter>(); });

builder.Services.AddApplicationInsights(builder.Environment.IsDevelopment(), builder.Configuration);

builder.Services.AddSingleton(new JsonSerializerOptions().ForFhirExtended());
builder.Services.AddHttpClients();
builder.Services.AddValidators();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ResponseMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
