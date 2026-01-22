using FluentValidation;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Models;
using static NWRI.eReferralsService.API.Constants.ValidationMessages;
namespace NWRI.eReferralsService.API.Validators;

public class BundleCancelReferralModelValidator : AbstractValidator<BundleCancelReferralModel>
{
    public BundleCancelReferralModelValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;

        RuleFor(x => x.MessageHeader!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(MessageHeader)))
            .ChildRules(messageHeader =>
            {
                messageHeader.RuleFor(x => x.Meta)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Meta)));

                messageHeader.RuleFor(x => x.Destination)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Destination)));

                messageHeader.RuleFor(x => x.Focus)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Focus)));

                messageHeader.RuleForEach(x => x.Focus)
                    .ChildRules(f =>
                    {
                        f.RuleFor(x => x.Reference)
                            .NotEmpty()
                            .WithMessage(MissingEntityField<MessageHeader>("focus.reference"));
                    });

                messageHeader.RuleFor(x => x.Reason)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Reason)));

                messageHeader.RuleFor(x => x.Reason!.Coding)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<MessageHeader>("reason.coding"));

                messageHeader.RuleFor(x => x.Sender)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Sender)));

                messageHeader.RuleFor(x => x.Sender!.Reference)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<MessageHeader>("sender.reference"));

                messageHeader.RuleFor(x => x.Source)
                    .NotNull()
                    .WithMessage(MissingEntityField<MessageHeader>(nameof(MessageHeader.Source)));
            });
        RuleFor(x => x.ServiceRequest!)
            .NotNull()
            .WithMessage(MissingBundleEntity(nameof(ServiceRequest)))
            .ChildRules(serviceRequest =>
            {
                serviceRequest.RuleFor(x => x.Meta)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Meta)));

                serviceRequest.RuleFor(x => x.Meta!.Profile)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<ServiceRequest>("meta.profile"));

                serviceRequest.RuleFor(x => x.Identifier)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Identifier)));

                serviceRequest.RuleFor(x => x.AuthoredOnElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.AuthoredOn)));

                serviceRequest.RuleFor(x => x.IntentElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Intent)));

                serviceRequest.RuleFor(x => x.Category)
                    .NotEmpty()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Category)));

                serviceRequest.RuleFor(x => x.Subject)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Subject)));

                serviceRequest.RuleFor(x => x.StatusElement)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>(nameof(ServiceRequest.Status)));

                serviceRequest.RuleFor(x => x.Occurrence)
                    .NotNull()
                    .WithMessage(MissingEntityField<ServiceRequest>("occurrencePeriod"));

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
        RuleFor(x => x.Organizations!)
            .NotEmpty()
            .WithMessage(MissingBundleEntity(nameof(Organization)))
            .Must(orgs => orgs is { Count: > 0 })
            .WithMessage(MissingBundleEntity(nameof(Organization)))
            .DependentRules(() =>
            {
                RuleFor(x => x.Organizations!)
                    .Must(orgs => orgs.Any(o => o.Identifier?.Any(id => !string.IsNullOrWhiteSpace(id.Value)) == true))
                    .WithMessage(MissingEntityField<Organization>(nameof(Organization.Identifier)));
            });
    }
}
