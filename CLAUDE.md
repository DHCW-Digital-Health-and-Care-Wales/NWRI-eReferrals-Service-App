# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NWRI eReferrals Service App is an ASP.NET Core 8.0 REST API that validates FHIR Bundles against UK Core and BARS profiles, then forwards referral creation/cancellation requests to the PAS (Patient Administration System) Referrals API.

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Build for release
dotnet build --configuration Release

# Run the application
dotnet run --project src/NWRI.eReferralsService.API

# Run all tests with coverage
dotnet test --configuration Release --collect "XPlat Code Coverage" --settings coverlet.runsettings

# Run unit tests only
dotnet test test/NWRI.eReferralsService.Unit.Tests/

# Run integration tests only
dotnet test test/NWRI.eReferralsService.Integration.Tests/

# Run a specific test by name
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

### Request Flow
1. `ReferralsController` receives HTTP requests
2. `HeadersModelValidator` validates required headers (X-Request-Id, X-Correlation-Id)
3. `BundleCreateReferralModelValidator` validates FHIR Bundle structure
4. `FhirBundleProfileValidator` validates against UK Core/BARS profiles
5. `ReferralService` determines workflow (Create/Cancel) and calls PAS API
6. `ResponseMiddleware` catches all exceptions, returns FHIR `OperationOutcome`

### Key Patterns
- **Options Pattern**: All configuration uses `IOptions<T>` with validation at startup via `OptionValidators/`
- **Resilience**: HTTP calls use Polly for retry with exponential backoff (configured in `ResilienceConfig`)
- **FHIR Validation**: Async validator with warmup service (`FhirBundleProfileValidatorWarmupService`) that pre-loads packages on startup
- **Error Handling**: All exceptions flow through `ResponseMiddleware` which converts them to FHIR `OperationOutcome` responses

### Workflow Determination
The API inspects `MessageHeader.reason.coding.code` and `ServiceRequest.status`:
- **Create**: `reason = new` AND `status = active`
- **Cancel**: `reason = update` AND `status` is `revoked` or `entered-in-error`

## Key Files

- `Program.cs` - DI setup, middleware registration, startup configuration
- `Controllers/ReferralsController.cs` - Two endpoints: `POST /$process-message`, `GET /ServiceRequest/{id}`
- `Services/ReferralService.cs` - Core business logic, workflow orchestration
- `Validators/FhirBundleProfileValidator.cs` - FHIR profile validation using Firely SDK
- `Middleware/ResponseMiddleware.cs` - Global exception handling, OperationOutcome generation
- `Extensions/ServiceCollectionExtensions.cs` - Service registration and resilience pipeline setup

## Testing

- **Framework**: xUnit with Moq for mocking and FluentAssertions for assertions
- **Data Generation**: AutoFixture for test data
- **HTTP Mocking**: RichardSzalay.MockHttp
- **Coverage**: Coverlet with thresholds (60% fail, 80% warn)
- **Test Data**: Example FHIR bundles in `test/NWRI.eReferralsService.Unit.Tests/TestData/`

## Configuration

Key settings in `appsettings.json`:
- `PasReferralsApi` - Base URL and endpoints for downstream PAS API
- `Resilience` - Retry policy, timeouts (30s total, 10s per attempt, 3 retries)
- `FhirBundleProfileValidation` - Enable/disable validation, concurrency limits
- `ManagedIdentity` - Azure managed identity client ID for authentication

## Health Checks

- `/health/live` - Liveness probe (FHIR validator ready)
- `/health/ready` - Readiness probe (app running)
- `/health` - Combined health status

## Local Development

1. Run `az login --tenant <YOUR_TENANT>`
2. Configure `appsettings.Development.json` with PAS API BaseUrl
3. `dotnet run --project src/NWRI.eReferralsService.API`
4. Open https://localhost:5069/swagger/index.html
