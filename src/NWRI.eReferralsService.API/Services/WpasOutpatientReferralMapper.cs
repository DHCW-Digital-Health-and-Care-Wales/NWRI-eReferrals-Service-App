using System.Globalization;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Services;

public sealed class WpasOutpatientReferralMapper : IWpasOutpatientReferralMapper
{
    private const string NhsNumberSystem = "https://fhir.nhs.uk/Id/nhs-number";
    private const string NhsNumberVerificationStatusSystem =
        "https://fhir.hl7.org.uk/CodeSystem/UKCore-NHSNumberVerificationStatus";
    private const string ServiceRequestCategorySystem = "https://fhir.nhs.uk/CodeSystem/message-category-servicerequest";
    private const string BarsUseCaseCategorySystem = "https://fhir.nhs.uk/CodeSystem/usecases-categories-bars";

    public WpasCreateReferralRequest Map(BundleCreateReferralModel createReferralModel)
    {
        var encounterId = createReferralModel.Encounter?.Identifier.First().Value!;
        var receiverOrganisationId = createReferralModel.Organizations?.First(x => x.Name == "Receiving/performing Organization").Identifier.First().Value!;
        var senderOrganisationId = createReferralModel.Organizations?.First(x => x.Name == "Sender Organization").Identifier.First().Value!;
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
            ContractDetails = new WpasCreateReferralRequest.ContractDetailsModel
            {
                ProviderOrganisationCode = receiverOrganisationId
            },
            PatientDetails = new WpasCreateReferralRequest.PatientDetailsModel
            {
                NhsNumber = nhsIdentifier.Value!,
                NhsNumberStatusIndicator = nhsNumberVerificationStatusCode!,
                PatientName = new WpasCreateReferralRequest.PatientDetailsModel.PatientNameModel
                {
                    Surname = patientName.Family!,
                    FirstName = patientName.Given.First()!
                },
                BirthDate = WpasDate(createReferralModel.Patient!.BirthDate),
                Sex = char.ToUpperInvariant(createReferralModel.Patient!.Gender!.Value.ToString()[0]).ToString(),
                UsualAddress = new WpasCreateReferralRequest.PatientDetailsModel.UsualAddressModel
                {
                    NoAndStreet = address.Line.First()!,
                    Town = address.City!,
                    Postcode = address.PostalCode!,
                    Locality = string.Empty
                }
            },
            ReferralDetails = new WpasCreateReferralRequest.ReferralDetailsModel
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
                MainSpecialty = "130",
                ReferrerPriorityType = "2",
                ReasonForReferral =
                    createReferralModel.Conditions!.First().Code!.Coding.First().Display!.Trim()[..8],
                ReferralIdentifier = encounterId
            }
        };
    }

    private static string WpasDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out var dto)
            ? dto.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : string.Empty;
}
