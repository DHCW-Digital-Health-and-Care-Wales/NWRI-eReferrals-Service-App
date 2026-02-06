# NWRI eReferrals Service App

## Description
This project is an ASP.NET Core API that provides endpoints for handling Referrals.

## Prerequisites
Make sure you have the following installed and set up:
- [.NET SDK](https://dotnet.microsoft.com/download) version 8.0
- `az login --tenant <YOUR_TENNANT>`

## Required configuration for local development
To configure the project, follow these steps:
1. Open [appsettings.Development.json](./src/NWRI.eReferralsService.API/appsettings.Development.json) or user secrets file and configure BaseUrl for WPAS API.
```
"WpasApi": {
    "BaseUrl": "<YOUR_URL>"
```

## Project Structure
The core project structure is organized as follows:
```
NWRI.eReferralsService.API/
│
├── Properties
│   └── launchSettings.json
|
├── Configuration
│   ├── Configuration files and their validation
│   ├── OptionValidators/
│   └── Resilience/
│
├── Constants
│   └── Application-wide constants and message definitions
│
├── Controllers
│   └── API controllers for handling HTTP requests
|
├── Errors
│   └── FHIR HTTP error models
|
├── Exceptions
│   └── Custom exception classes
|
├── Extensions
│   └── Extension methods for services and utilities
|
├── FhirPackages
│   └── FHIR profile packages for validation
|
├── Helpers
│   └── Helper classes for common operations
|
├── Middleware
│   └── Response finalisation and error handling process
|
├── Models
│   └── Data models and DTOs
|
├── Services
│   └── Service classes implementing business logic
|
├── Swagger
│   └── Swagger configuration and example definitions
│
├── Validators
│   └── Validation classes for request validation
|
├── appsettings.json
|   └── appsettings.Development.json
|
└── Program.cs
```

## Running the Project
To run the project locally, follow these steps:
1. Clone the repository.
2. Don't forget `az login --tenant <YOUR_TENNANT>`.
3. Setup local configuration according to `Required configuration for local development` section.
4. Rebuild and run the project.
5. The service will warm up the FHIR validator during startup (check logs for confirmation).
6. Open your web browser and navigate to `https://localhost:5069/swagger/index.html` to access the SwaggerUI with API endpoints.

## Error Handling and Middleware
The service uses `ResponseMiddleware` to handle all exceptions and return properly formatted FHIR `OperationOutcome` responses.

**Error response format**: All errors return a FHIR `OperationOutcome` JSON object with detailed diagnostic information.

**Exception handling**:
- **Header validation errors** → `400 Bad Request` with validation details
- **Bundle deserialization errors** (invalid JSON) → `400 Bad Request` 
- **Bundle validation errors** (invalid structure/workflow) → `400 Bad Request`
- **FHIR profile validation errors** → `400 Bad Request` with specific profile violations
- **Request parameter validation errors** (e.g., invalid GUID) → `400 Bad Request`
- **External API errors** (WPAS API returns 500) → `503 Service Unavailable`
- **Network errors** (WPAS API unreachable) → `503 Service Unavailable`
- **Timeout errors** (after all retries) → `503 Service Unavailable`
- **Unknown errors** → `500 Internal Server Error`

**Response headers**: Every response includes:
- `X-Correlation-Id` - for request tracing across systems
- Standard content type headers

## Health Checks
The service provides three health check endpoints for Kubernetes liveness and readiness probes:

### Endpoints
- **`/health/live`** - Liveness probe - checks if the application is running
  - Returns `200 OK` once the application starts
  - Returns `503 Service Unavailable` when the validator is still warming up

- **`/health/ready`** - Readiness probe - checks if the FHIR Bundle Profile Validator is initialized and ready
  - Always returns `200 OK` when the validator is ready

- **`/health`** - General health check - combines all health checks
  - Returns `200 OK` when all checks pass
  - Returns `503 Service Unavailable` if any check fails

### Startup Behavior
1. **FHIR Bundle Profile Validator Warmup**: 
   - The service initializes the FHIR Bundle Profile Validator during startup via `FhirBundleProfileValidatorWarmupService`
   - This process loads FHIR packages and profiles, which may take several seconds
   - During warmup, `/health/live` will NOT return any status and the application will not respond to requests
   - Once complete, the service logs: `"[Startup] FHIR-Bundle-Profile-Validator warmup complete. Application is ready to accept requests"`
   - **Note**: If `FhirBundleProfileValidation.Enabled` is set to `false` in configuration, the warmup is skipped and a warning is logged

## FHIR Bundle Profile Validation
The service validates incoming FHIR Bundles against UK Core and BARS profiles. Configuration in [appsettings.json](./src/NWRI.eReferralsService.API/appsettings.json):
```json
"FhirBundleProfileValidation": {
  "Enabled": true,                    // Enable/disable FHIR profile validation
  "MaxConcurrentValidations": 4,      // Maximum number of concurrent validations
  "ValidationTimeoutSeconds": 10      // Timeout for a single validation operation
}
```

**Note**: When validation is enabled, the service will reject requests with `400 Bad Request` if the FHIR Bundle does not conform to the required profiles.

## Resilience
All HTTP requests to external services (WPAS API) use a resilience policy with automatic retry and timeout. Configuration in [appsettings.json](./src/NWRI.eReferralsService.API/appsettings.json):
```json
"Resilience": {
  "TotalTimeoutSeconds": 30,         // Total execution timeout (includes all retries)
  "AttemptTimeoutSeconds": 10,       // Timeout of a single request attempt
  "Retry": {
    "IsExponentialDelay": true,      // Is delay between retries exponential?
    "DelaySeconds": 2,               // Initial delay between retries (increases exponentially)
    "MaxRetries": 3                  // Maximum number of retry attempts
  }
}
```

**Retry behavior**:
- Retries are triggered on: `408 Request Timeout`, `429 Too Many Requests`, `500 Internal Server Error`, `502 Bad Gateway`, `503 Service Unavailable`, `504 Gateway Timeout`, or network exceptions
- With exponential delay and `DelaySeconds: 2`, the delays will be approximately: 2s, 4s, 8s
- Total time for all attempts: up to `TotalTimeoutSeconds` (30s by default)
- After all retries exhausted, the service returns `503 Service Unavailable` to the client

## API Endpoints
Example payloads, responses and errors can be found in the `Swagger/Examples` folder. 

### POST /$process-message

#### Description
Accepts a FHIR `Bundle` message and processes it as a referral workflow.
Depending on the message content, the API will either:

- **Create** a new referral, or
- **Cancel** an existing referral

#### Request details
Request body must be a valid FHIR `Bundle` JSON object.
See [Example Payload](./src/NWRI.eReferralsService.API/Swagger/Examples/process-message-payload-response.json).

#### Workflow determination
The API determines the workflow action by inspecting:

- `MessageHeader.reason.coding.code`
- `ServiceRequest.status`

Supported combinations:

- **Create**: `reason = new` AND `status = active`
- **Cancel**: `reason = update` AND `status` is (`revoked` or `entered-in-error`)

If either field is missing, or the combination does not match the supported set, the endpoint returns `400`.

#### Responses
  - 200 - Referral processed successfully (Create). Returns an enriched FHIR `Bundle`. [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/process-message-payload&response.json)
  - 400 - Request validation failed (e.g. invalid/missing headers, invalid JSON/bundle, FHIR profile/mandatory data validation, or unsupported reason/status combination). [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/process-message-bad-request.json)
  - 429 - Too many requests. [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/common-too-many-requests.json)
  - 500 - Internal error. [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/common-internal-server-error.json)
  - 503 - WPAS API unavailable or returned 500. [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/common-external-server-error.json)

### GET /ServiceRequest/&#123;id&#125;

#### Description
Gets a referral by **id**.

#### Request details
Route parameter **id** should be a valid GUID.

#### Responses
  - 200 - FHIR Bundle generated from DB data. [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/get-referral-ok-response.json)
  - 400 - Headers of id validation errors [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/get-referral-bad-request.json)
  - 404 - Referral with provided id wasn't found [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/get-referral-not-found.json)
  - 429 - Too many requests [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/common-too-many-requests.json)
  - 500 - Internal error [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/common-internal-server-error.json)
  - 503 - WPAS API Unavailable or returned 500 [Example](./src/NWRI.eReferralsService.API/Swagger/Examples/common-external-server-error.json)
