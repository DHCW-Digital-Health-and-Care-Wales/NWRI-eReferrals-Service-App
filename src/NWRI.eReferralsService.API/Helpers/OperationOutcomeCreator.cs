using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;

namespace NWRI.eReferralsService.API.Helpers;

public static class OperationOutcomeCreator
{
    public static OperationOutcome CreateOperationOutcome(params BaseFhirHttpError[] errors)
    {
        var issues = errors.Select(error =>
        {
            var issue = new OperationOutcome.IssueComponent
            {
                Severity = OperationOutcome.IssueSeverity.Error,
                Code = error.IssueType,
                Details = new CodeableConcept(BaseFhirHttpError.System, error.Code, error.Display),
            };

            if (!string.IsNullOrWhiteSpace(error.DiagnosticsMessage))
                issue.Diagnostics = error.DiagnosticsMessage;

            return issue;
        }).ToList();

        return new OperationOutcome
        {
            Id = Guid.NewGuid().ToString(),
            Meta = new Meta { Profile = [FhirConstants.OperationOutcomeProfile] },
            Issue = issues
        };
    }

    public static OperationOutcome CreateOperationOutcome(BaseFhirException fhirException)
    {
        return CreateOperationOutcome(fhirException.Errors.ToArray());
    }
}
