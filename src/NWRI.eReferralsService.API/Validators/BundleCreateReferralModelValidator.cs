using FluentValidation;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Models;
using static NWRI.eReferralsService.API.Constants.ValidationMessages;
// ReSharper disable NullableWarningSuppressionIsUsed

namespace NWRI.eReferralsService.API.Validators;

public class BundleCreateReferralModelValidator : AbstractValidator<BundleCreateReferralModel>
{
    private const string OccurrencePeriod = "occurrencePeriod";
    private const string EventCoding = "eventCoding";

    public BundleCreateReferralModelValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;

        RuleFor(x => x.MessageHeader!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(MessageHeader)))
            .ChildRules(messageHeader =>
            {
                messageHeader.RuleFor(x => x.Definition)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Definition)));

                messageHeader.RuleFor(x => x.Meta)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Meta)));

                messageHeader.RuleFor(x => x.Destination)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Destination)));

                messageHeader.RuleFor(x => x.Sender)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Sender)));

                messageHeader.RuleFor(x => x.Source)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Source)));

                messageHeader.RuleFor(x => x.Event)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(EventCoding));

                messageHeader.RuleFor(x => x.Reason)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Reason)));

                messageHeader.RuleFor(x => x.Focus)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Focus)));
            });

        RuleFor(x => x.ServiceRequest!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(ServiceRequest)))
            .ChildRules(serviceRequest =>
            {
                serviceRequest.RuleFor(x => x.StatusElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Status)));

                serviceRequest.RuleFor(x => x.IntentElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Intent)));

                serviceRequest.RuleFor(x => x.Subject)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Subject)));

                serviceRequest.RuleFor(x => x.Encounter)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Encounter)));

                serviceRequest.RuleFor(x => x.AuthoredOnElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.AuthoredOn)));

                serviceRequest.RuleFor(x => x.BasedOn)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.BasedOn)));

                serviceRequest.RuleFor(x => x.Occurrence)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(OccurrencePeriod));

                serviceRequest.RuleFor(x => x.Requester)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Requester)));

                serviceRequest.RuleFor(x => x.Performer)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Performer)));

                serviceRequest.RuleFor(x => x.Category)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Category)));

                serviceRequest.RuleFor(x => x.Meta)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Meta)));
            });

        RuleFor(x => x.Encounter!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(Encounter)))
            .ChildRules(encounter =>
            {
                encounter.RuleFor(x => x.Identifier)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<Encounter>(nameof(Encounter.Identifier)));

                encounter.RuleFor(x => x.StatusElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<Encounter>(nameof(Encounter.Status)));

                encounter.RuleFor(x => x.Class)
                    .NotNull()
                    .WithMessage(MissingEntityField<Encounter>(nameof(Encounter.Class)));

                encounter.RuleFor(x => x.Subject)
                    .NotNull()
                    .WithMessage(MissingEntityField<Encounter>(nameof(Encounter.Subject)));

                encounter.RuleFor(x => x.Period)
                    .NotNull()
                    .WithMessage(MissingEntityField<Encounter>(nameof(Encounter.Period)));

                encounter.RuleFor(x => x.Meta)
                    .NotNull()
                    .WithMessage(MissingEntityField<Encounter>(nameof(Encounter.Meta)));
            });

        RuleFor(x => x.CarePlan!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(CarePlan)))
            .ChildRules(carePlan =>
            {
                carePlan.RuleFor(x => x.StatusElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<CarePlan>(nameof(CarePlan.Status)));

                carePlan.RuleFor(x => x.IntentElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<CarePlan>(nameof(CarePlan.Intent)));

                carePlan.RuleFor(x => x.Activity)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<CarePlan>(nameof(CarePlan.Activity)));

                carePlan.RuleFor(x => x.Subject)
                    .NotNull()
                    .WithMessage(MissingEntityField<CarePlan>(nameof(CarePlan.Subject)));

                carePlan.RuleFor(x => x.Encounter)
                    .NotNull()
                    .WithMessage(MissingEntityField<CarePlan>(nameof(CarePlan.Encounter)));

                carePlan.RuleFor(x => x.Addresses)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<CarePlan>(nameof(CarePlan.Addresses)));

                carePlan.RuleFor(x => x.Meta)
                    .NotNull()
                    .WithMessage(MissingEntityField<CarePlan>(nameof(CarePlan.Meta)));
            });

        RuleFor(x => x.HealthcareService!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(HealthcareService)))
            .ChildRules(healthcareService =>
            {
                healthcareService.RuleFor(x => x.ActiveElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<HealthcareService>(nameof(HealthcareService.Active)));

                healthcareService.RuleFor(x => x.Identifier)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<HealthcareService>(nameof(HealthcareService.Identifier)));

                healthcareService.RuleFor(x => x.Meta)
                    .NotNull()
                    .WithMessage(MissingEntityField<HealthcareService>(nameof(HealthcareService.Meta)));

                healthcareService.RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<HealthcareService>(nameof(HealthcareService.Name)));

                healthcareService.RuleFor(x => x.ProvidedBy)
                    .NotNull()
                    .WithMessage(MissingEntityField<HealthcareService>(nameof(HealthcareService.ProvidedBy)));
            });

        RuleFor(x => x.Patient!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(Patient)))
            .ChildRules(patient =>
            {
                patient.RuleFor(x => x.Identifier)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<Patient>(nameof(Patient.Identifier)));

                patient.RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<Patient>(nameof(Patient.Name)));

                patient.RuleFor(x => x.BirthDate)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<Patient>(nameof(Patient.BirthDate)));

                patient.RuleFor(x => x.GenderElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<Patient>(nameof(Patient.Gender)));

                patient.RuleFor(x => x.Address)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<Patient>(nameof(Patient.Address)));
            });
    }
}
