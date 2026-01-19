using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Extensions;
using Task = Hl7.Fhir.Model.Task;

namespace WCCG.eReferralsService.API.Models;

public class BundleCreateReferralModel
{
    public required MessageHeader? MessageHeader { get; set; }
    public required ServiceRequest? ServiceRequest { get; set; }
    public required Patient? Patient { get; set; }
    public required Encounter? Encounter { get; set; }
    public required CarePlan? CarePlan { get; set; }
    public Location? IncidentLocation { get; set; }
    public List<Location>? Locations { get; set; }
    public required List<Organization>? Organizations { get; set; }
    public required List<Practitioner>? Practitioners { get; set; }
    public required List<PractitionerRole>? PractitionerRoles { get; set; }
    public List<Observation>? Observations { get; set; }
    public List<Flag>? SceneSafetyFlags { get; set; }
    public List<Flag>? Flags { get; set; }
    public List<MedicationStatement>? MedicationStatements { get; set; }
    public List<AllergyIntolerance>? AllergyIntolerances { get; set; }
    public List<Questionnaire>? Questionnaires { get; set; }
    public List<QuestionnaireResponse>? QuestionnaireResponses { get; set; }
    public required List<Consent>? Consents { get; set; }
    public required HealthcareService? HealthcareService { get; set; }
    public List<Condition>? Conditions { get; set; }
    public List<Task>? Tasks { get; set; }
    public List<Communication>? Communications { get; set; }
    public List<Procedure>? Procedures { get; set; }

    public static BundleCreateReferralModel FromBundle(Bundle bundle)
    {
        return new BundleCreateReferralModel
        {
            MessageHeader = bundle.ResourceByType<MessageHeader>(),
            ServiceRequest = bundle.ResourceByType<ServiceRequest>(),
            Patient = bundle.ResourceByType<Patient>(),
            Encounter = bundle.ResourceByType<Encounter>(),
            CarePlan = bundle.ResourceByType<CarePlan>(),
            IncidentLocation = bundle.ResourcesByProfile<Location>(FhirConstants.BarsLocationIncidentLocation).SingleOrDefault(),
            Locations = bundle.ResourcesExcludingProfile<Location>(FhirConstants.BarsLocationIncidentLocation).ToList(),
            Organizations = bundle.ResourcesByType<Organization>().ToList(),
            Practitioners = bundle.ResourcesByType<Practitioner>().ToList(),
            PractitionerRoles = bundle.ResourcesByType<PractitionerRole>().ToList(),
            Observations = bundle.ResourcesByType<Observation>().ToList(),
            SceneSafetyFlags = bundle.ResourcesByProfile<Flag>(FhirConstants.BarsFlagSceneSafety).ToList(),
            Flags = bundle.ResourcesExcludingProfile<Flag>(FhirConstants.BarsFlagSceneSafety).ToList(),
            MedicationStatements = bundle.ResourcesByType<MedicationStatement>().ToList(),
            AllergyIntolerances = bundle.ResourcesByType<AllergyIntolerance>().ToList(),
            Questionnaires = bundle.ResourcesByType<Questionnaire>().ToList(),
            QuestionnaireResponses = bundle.ResourcesByType<QuestionnaireResponse>().ToList(),
            Consents = bundle.ResourcesByType<Consent>().ToList(),
            HealthcareService = bundle.ResourceByType<HealthcareService>(),
            Conditions = bundle.ResourcesByType<Condition>().ToList(),
            Tasks = bundle.ResourcesByType<Task>().ToList(),
            Communications = bundle.ResourcesByType<Communication>().ToList(),
            Procedures = bundle.ResourcesByType<Procedure>().ToList()
        };
    }
}
