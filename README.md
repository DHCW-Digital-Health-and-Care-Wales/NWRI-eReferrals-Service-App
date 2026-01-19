# eReferrals API

## Description
This project is an ASP.NET Core API that provides endpoints for handling Referrals.

## Prerequisites
Make sure you have the following installed and set up:
- [.NET SDK](https://dotnet.microsoft.com/download) version 8.0
- `az login --tenant <YOUR_TENNANT>`

## Required configuration for local development
To configure the project, follow these steps:
1. Open [appsettings.Development.json](./src/WCCG.eReferralsService.API/appsettings.Development.json) or user secrets file and configure BaseUrl for PAS Referrals API.
```
"PasReferralsApi": {
    "BaseUrl": "<YOUR_URL>"
```

## Project Structure
The core project structure is organized as follows:
```
WCCG.PAS.Referrals.API/
│
├── Properties
│   └── launchSettings.json
|
├── Configuration
│   └── Configuration files and their validation
│
├── Controllers
│   └── v1
|       └── Controllers for API of version 1
|
├── Errors
│   └── FHIR HTTP error models
|
├── Middleware
│   └── Response finalisation and error handling process
|
├── Services
│   └── Service classes
|
├── Swagger
│   └── Helper classes for Swagger
│
├── Validators
│   └── Validation classes
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
5. Open your web browser and navigate to `https://localhost:xxxxx/swagger/index.html` to access the SwaggerUI with API endpoints.

## Resilience
All HTTP requests using resilience policy, which can be configured in [appsettings.json](./src/WCCG.eReferralsService.API/appsettings.json):
```
 "Resilience": {
    "TotalTimeoutSeconds": 30, // Totat execution timeout
    "AttemptTimeoutSeconds": 10, // Timeout of a single request attempt
    "Retry": {
      "IsExponentialDelay": true, // Is delay between retries increases exponentialy?
      "DelaySeconds": 2, // Delay between retries (first delay, when IsExponentialDelay is true, as next delay will be longer)
      "MaxRetries": 3 // Maximum number of retries
    }
  }
```

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
See [Example Payload](./src/WCCG.eReferralsService.API/Swagger/Examples/process-message-payload&response.json).

#### Workflow determination
The API determines the workflow action by inspecting:

- `MessageHeader.reason.coding.code`
- `ServiceRequest.status`

Supported combinations:

- **Create**: `reason = new` AND `status = active`
- **Cancel**: `reason = update` AND `status` is (`revoked` or `entered-in-error`)

If either field is missing, or the combination does not match the supported set, the endpoint returns `400`.

#### Responses
  - 200 - Referral processed successfully (Create). Returns an enriched FHIR `Bundle`. [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/process-message-payload&response.json)
  - 400 - Request validation failed (e.g. invalid/missing headers, invalid JSON/bundle, FHIR profile/mandatory data validation, or unsupported reason/status combination). [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/process-message-bad-request.json)
  - 429 - Too many requests. [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/common-too-many-requests.json)
  - 500 - Internal error. [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/common-internal-server-error.json)
  - 503 - PAS API unavailable or returned 500. [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/common-external-server-error.json)

### GET /ServiceRequest/&#123;id&#125;

#### Description
Gets a referral by **id**.

#### Request details
Route parameter **id** should be a valid GUID.

#### Responses
  - 200 - FHIR Bundle generated from DB data. [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/get-referral-ok-response.json)
  - 400 - Headers of id validation errors [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/get-referral-bad-request.json)
  - 404 - Referral with provided id wasn't found [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/get-referral-not-found.json)
  - 429 - Too many requests [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/common-too-many-requests.json)
  - 500 - Internal error [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/common-internal-server-error.json)
  - 503 - PAS API Unavailable or returned 500 [Example](./src/WCCG.eReferralsService.API/Swagger/Examples/common-external-server-error.json)

