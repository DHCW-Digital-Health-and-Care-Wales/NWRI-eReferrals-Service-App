using System.Text.Json;
using Microsoft.Extensions.Options;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Configuration.OptionValidators;
using NWRI.eReferralsService.API.Configuration.Resilience;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Helpers;
using NWRI.eReferralsService.API.Middleware;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.API.Swagger;
using NWRI.eReferralsService.API.Validators;

var builder = WebApplication.CreateBuilder(args);

//WpasApiConfig
builder.Services.AddOptions<WpasApiConfig>().Bind(builder.Configuration.GetSection(WpasApiConfig.SectionName));
builder.Services.AddSingleton<IValidateOptions<WpasApiConfig>, ValidateWpasApiConfigOptions>();

//Resilience
builder.Services.AddOptions<ResilienceConfig>().Bind(builder.Configuration.GetSection(ResilienceConfig.SectionName));
builder.Services.AddSingleton<IValidateOptions<ResilienceConfig>, ValidateResilienceConfigOptions>();

builder.Services.AddOptions<FhirBundleProfileValidationConfig>().Bind(builder.Configuration.GetSection(FhirBundleProfileValidationConfig.SectionName));
builder.Services.AddSingleton<IValidateOptions<FhirBundleProfileValidationConfig>, ValidateFhirBundleProfileValidationOptions>();
builder.Services.AddSingleton<IFhirBundleProfileValidator, FhirBundleProfileValidator>();
builder.Services.AddSingleton<IEventLogger, EventLogger>();
builder.Services.AddSingleton<Base64Decoder>();
builder.Services.AddSingleton<IHeadersDecoder, HeadersDecoder>();

builder.Services.AddHostedService<FhirBundleProfileValidatorWarmupService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options => { options.OperationFilter<SwaggerOperationFilter>(); });

builder.Services.AddApplicationInsights(builder.Environment.IsDevelopment(), builder.Configuration);

builder.Services.AddSingleton(new JsonSerializerOptions().ForFhirExtended());
builder.Services.AddHttpClients();
builder.Services.AddValidators();

builder.Services.AddCustomHealthChecks();

var app = builder.Build();

app.UseMiddleware<RequestResponseAuditMiddleware>();
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

app.MapCustomHealthChecks();

app.Run();
