using System.Globalization;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Models;
using NWRI.eReferralsService.API.Models.WPAS;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Services;

public sealed class WpasOutpatientReferralMapper : IWpasOutpatientReferralMapper
{
    public WpasOutpatientReferralRequest Map(BundleCreateReferralModel createReferralModel)
    {
        var encounterId = createReferralModel.Encounter?.Identifier.First().Value!;
        var receiverOrganisationId = createReferralModel.Organizations?.First(x => x.Name == "Receiver Organization").Identifier.First().Value!;
        var patientName = createReferralModel.Patient?.Name.First();
        var address = createReferralModel.Patient?.Address.First();

        return new WpasOutpatientReferralRequest
        {
            RecordId = encounterId,
            ContractDetails = new WpasOutpatientReferralRequest.ContractDetailsModel
            {
                ProviderOrganisationCode = receiverOrganisationId
            },
            PatientDetails = new WpasOutpatientReferralRequest.PatientDetailsModel
            {
                NhsNumber = createReferralModel.Patient?
                    .Identifier
                    .First(x => x.System == "https://fhir.nhs.uk/Id/nhs-number").Value!,
                // TODO: TBC - What is the mapping for this field?
                NhsNumberStatusIndicator = string.Empty,
                PatientName = new WpasOutpatientReferralRequest.PatientDetailsModel.PatientNameModel
                {
                    Surname = patientName?.Family!,
                    FirstName = patientName?.Given.First()!
                },
                BirthDate = WpasDate(createReferralModel.Patient?.BirthDate),
                Sex = MapGender(createReferralModel.Patient?.Gender),
                UsualAddress = new WpasOutpatientReferralRequest.PatientDetailsModel.UsualAddressModel
                {
                    NoAndStreet = address?.Line.FirstOrDefault() ?? string.Empty,
                    Town = address?.City ?? string.Empty,
                    Postcode = address?.PostalCode ?? string.Empty,
                    Locality = string.Empty
                }
            },
            ReferralDetails = new WpasOutpatientReferralRequest.ReferralDetailsModel
            {
                OutpatientReferralSource = "15",
                ReferringOrganisationCode = receiverOrganisationId,
                ServiceTypeRequested = "6",
                ReferrerCode = createReferralModel.ServiceRequest?.Requester?.Identifier?.Value!,
                AdministrativeCategory = "01",
                DateOfReferral = WpasDate(createReferralModel.ServiceRequest?.AuthoredOn),
                // TODO: TBC - Fixed value: 130 or 460 ?
                MainSpecialty = "130",
                // TODO: TBC - What is the mapping for this field?
                ReferrerPriorityType = string.Empty,
                // TODO: TBC - What is the exact mapping for this field?
                ReasonForReferral = createReferralModel.Conditions?.First().Code?.Text!,
                ReferralIdentifier = encounterId
            }
        };
    }

    // TODO: TBC - What are the possible values
    private static string MapGender(AdministrativeGender? gender)
    {
        return gender switch
        {
            AdministrativeGender.Female => "F",
            AdministrativeGender.Male => "M",
            AdministrativeGender.Other => "O",
            _ => "U"
        };
    }

    private static string WpasDate(string? value) =>
        DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, out var dto)
            ? dto.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : string.Empty;
}
