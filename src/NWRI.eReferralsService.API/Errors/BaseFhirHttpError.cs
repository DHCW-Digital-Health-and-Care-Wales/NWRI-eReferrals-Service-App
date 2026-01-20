using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Helpers;

namespace WCCG.eReferralsService.API.Errors;

public abstract class BaseFhirHttpError
{
    public static string System => FhirConstants.HttpErrorCodesSystem;
    public string Display => FhirHttpErrorHelper.GetDisplayMessageByCode(Code);
    public abstract string Code { get; }
    public abstract string DiagnosticsMessage { get; }
    public abstract OperationOutcome.IssueType IssueType { get; }
}
