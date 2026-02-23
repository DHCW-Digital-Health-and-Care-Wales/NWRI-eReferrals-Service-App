using System.Globalization;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Mappers;

public sealed class WpasCreateReferralRequestMapper
{
    private const string NhsNumberSystem = "https://fhir.nhs.uk/Id/nhs-number";
    private const string NhsNumberVerificationStatusSystem =
        "https://fhir.hl7.org.uk/CodeSystem/UKCore-NHSNumberVerificationStatus";
    private const string ServiceRequestCategorySystem = "https://fhir.nhs.uk/CodeSystem/message-category-servicerequest";
    private const string BarsUseCaseCategorySystem = "https://fhir.nhs.uk/CodeSystem/usecases-categories-bars";

    private const string ReceivingPerformingOrganisationName = "Receiving/performing Organization";
    private const string SenderOrganisationName = "Sender Organization";
    private const string UrgentReferrerPriorityType = "2";
    private const string OphthalmologyMainSpecialtyCode = "130";

    public WpasCreateReferralRequest Map(BundleCreateReferralModel createReferralModel)
    {
        var encounterId = createReferralModel.Encounter?.Identifier.First().Value!;
        var receiverOrganisationId = createReferralModel.Organizations?
            .First(x => StringComparer.InvariantCultureIgnoreCase.Equals(x.Name, ReceivingPerformingOrganisationName))
            .Identifier.First().Value!;
        var senderOrganisationId = createReferralModel.Organizations?
            .First(x => StringComparer.InvariantCultureIgnoreCase.Equals(x.Name, SenderOrganisationName))
            .Identifier.First().Value!;

        var patientName = createReferralModel.Patient!.Name.First();
        var address = createReferralModel.Patient!.Address.First();
        var nhsIdentifier = createReferralModel.Patient!
            .Identifier
            .First(x => string.Equals(x.System, NhsNumberSystem, StringComparison.OrdinalIgnoreCase));

        var nhsNumberVerificationStatusCode = nhsIdentifier
            .Extension
            .Select(e => e.Value as CodeableConcept)
            .Where(cc => cc is not null)
            .SelectMany(cc => cc!.Coding)
            .FirstOrDefault(c => string.Equals(c.System, NhsNumberVerificationStatusSystem, StringComparison.OrdinalIgnoreCase))
            ?.Code;

        return new WpasCreateReferralRequest
        {
            RecordId = encounterId,
            ContractDetails = new ContractDetails
            {
                ProviderOrganisationCode = receiverOrganisationId
            },
            PatientDetails = new PatientDetails
            {
                NhsNumber = nhsIdentifier.Value!,
                NhsNumberStatusIndicator = nhsNumberVerificationStatusCode!,
                PatientName = new PatientName
                {
                    Surname = patientName.Family!,
                    FirstName = patientName.Given.First()!
                },
                BirthDate = WpasDate(createReferralModel.Patient!.BirthDate),
                Sex = char.ToUpperInvariant(createReferralModel.Patient!.Gender!.Value.ToString()[0]).ToString(),
                UsualAddress = new UsualAddress
                {
                    NoAndStreet = address.Line.First()!,
                    Town = address.City!,
                    Postcode = address.PostalCode!,
                    Locality = string.Empty
                }
            },
            ReferralDetails = new ReferralDetails
            {
                OutpatientReferralSource = senderOrganisationId,
                ReferringOrganisationCode = receiverOrganisationId,
                ServiceTypeRequested = createReferralModel.ServiceRequest?.Category
                                           .SelectMany(c => c.Coding)
                                           .First(c => string.Equals(c.System, ServiceRequestCategorySystem, StringComparison.OrdinalIgnoreCase))
                                           .Code!,
                ReferrerCode = createReferralModel.Practitioners?.First()
                    .Identifier.First()
                    .Value!,
                AdministrativeCategory = createReferralModel.ServiceRequest?.Category
                                             .SelectMany(c => c.Coding)
                                             .First(c => string.Equals(c.System, BarsUseCaseCategorySystem, StringComparison.OrdinalIgnoreCase))
                                             .Code!,
                DateOfReferral = WpasDate(createReferralModel.ServiceRequest?.AuthoredOn),
                MainSpecialty = OphthalmologyMainSpecialtyCode,
                ReferrerPriorityType = UrgentReferrerPriorityType,
                ReasonForReferral = Truncate(
                    createReferralModel.Conditions!.First().Code!.Coding.First().Display!, 8),
                ReferralIdentifier = encounterId
            }
        };
    }

    private static string Truncate(string value, int maxLength)
    {
        value = value.Trim();
        return value[..Math.Min(maxLength, value.Length)];
    }

    private static string WpasDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out var dto)
            ? dto.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : string.Empty;
}
