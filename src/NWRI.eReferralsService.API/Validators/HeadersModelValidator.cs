using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using Hl7.Fhir.Model;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Extensions;
using NWRI.eReferralsService.API.Models;

namespace NWRI.eReferralsService.API.Validators;

public partial class HeadersModelValidator : AbstractValidator<HeadersModel>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions().ForFhirExtended();

    [GeneratedRegex(@"([a-zA-Z0-9-]+\|?)+", RegexOptions.CultureInvariant)]
    private static partial Regex ValidUseCaseRegex();

    private const string AcceptTypePart = "application/fhir+json";
    private const string AcceptVersionPart = "version=1.2.0";

    public HeadersModelValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Continue;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.TargetIdentifier)
            //Empty
            .NotEmpty()
            .WithMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.TargetIdentifier))
            .WithErrorCode(nameof(ValidationErrorCode.MissingRequiredHeaderCode))
            //Format
            .Must(x => BeValidFhirType<Identifier>(x.AsSpan()))
            .WithMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.TargetIdentifier, nameof(Identifier)))
            .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));

        RuleFor(x => x.EndUserOrganisation)
            //Empty
            .NotEmpty()
            .WithMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.EndUserOrganisation))
            .WithErrorCode(nameof(ValidationErrorCode.MissingRequiredHeaderCode))
            //Format
            .Must(x => BeValidFhirType<Organization>(x.AsSpan()))
            .WithMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.EndUserOrganisation, nameof(Organization)))
            .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));

        RuleFor(x => x.RequestingSoftware)
            //Empty
            .NotEmpty()
            .WithMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.RequestingSoftware))
            .WithErrorCode(nameof(ValidationErrorCode.MissingRequiredHeaderCode))
            //Format
            .Must(x => BeValidFhirType<Device>(x.AsSpan()))
            .WithMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.RequestingSoftware, nameof(Device)))
            .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));

        When(x => !string.IsNullOrWhiteSpace(x.RequestingPractitioner), () =>
        {
            RuleFor(x => x.RequestingPractitioner)
                //Format
                .Must(x => BeValidFhirType<PractitionerRole>(x.AsSpan()))
                .WithMessage(ValidationMessages.InvalidFhirObject(RequestHeaderKeys.RequestingPractitioner, nameof(PractitionerRole)))
                .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));
        });

        RuleFor(x => x.RequestId)
            //Empty
            .NotEmpty()
            .WithMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.RequestId))
            .WithErrorCode(nameof(ValidationErrorCode.MissingRequiredHeaderCode))
            // Format
            .Must(BeValidGuid)
            .WithMessage(ValidationMessages.NotGuidFormat(RequestHeaderKeys.RequestId))
            .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));

        RuleFor(x => x.CorrelationId)
            //Empty
            .NotEmpty()
            .WithMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.CorrelationId))
            .WithErrorCode(nameof(ValidationErrorCode.MissingRequiredHeaderCode))
            //Format
            .Must(BeValidGuid)
            .WithMessage(ValidationMessages.NotGuidFormat(RequestHeaderKeys.CorrelationId))
            .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));

        RuleFor(x => x.UseContext)
            //Empty
            .NotEmpty()
            .WithMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.UseContext))
            .WithErrorCode(nameof(ValidationErrorCode.MissingRequiredHeaderCode))
            //Format
            .Must(ContainValidUseCaseValues)
            .WithMessage(ValidationMessages.NotExpectedFormat(RequestHeaderKeys.UseContext,
                RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.UseContext)))
            .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));

        RuleFor(x => x.Accept)
            //Empty
            .NotEmpty()
            .WithMessage(ValidationMessages.MissingRequiredHeader(RequestHeaderKeys.Accept))
            .WithErrorCode(nameof(ValidationErrorCode.MissingRequiredHeaderCode))
            //Format
            .Must(BeValidAcceptValue)
            .WithMessage(ValidationMessages.NotExpectedFormat(RequestHeaderKeys.Accept,
                RequestHeaderKeys.GetExampleValue(RequestHeaderKeys.Accept)))
            .WithErrorCode(nameof(ValidationErrorCode.InvalidHeaderCode));
    }

    private static bool BeValidGuid(string? value)
    {
        return Guid.TryParse(value, out _);
    }

    private bool BeValidFhirType<T>(ReadOnlySpan<char> value) where T : Base
    {
        var maxSize = Base64.GetMaxDecodedFromUtf8Length(value.Length);
        var rentedBuffer = ArrayPool<byte>.Shared.Rent(maxSize);

        try
        {
            if (Convert.TryFromBase64Chars(value, rentedBuffer, out var writtenBytes))
            {
                var jsonBytes = new ReadOnlySpan<byte>(rentedBuffer, 0, writtenBytes);
                JsonSerializer.Deserialize<T>(jsonBytes, _jsonSerializerOptions);

                return true;
            }
        }
        catch
        {
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }

        return false;
    }

    private static bool ContainValidUseCaseValues(string? value)
    {
        return value != null && ValidUseCaseRegex().IsMatch(value);
    }

    private static bool BeValidAcceptValue(string? value)
    {
        var valueSpan = value.AsSpan();

        var separatorIndex = valueSpan.IndexOf(';');
        if (separatorIndex < 0 || valueSpan.Count(';') > 1)
        {
            return false;
        }

        var firstPart = valueSpan[..separatorIndex].Trim();
        var secondPart = valueSpan[(separatorIndex + 1)..].Trim();

        return
            (firstPart.Equals(AcceptTypePart.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
             secondPart.Equals(AcceptVersionPart.AsSpan(), StringComparison.OrdinalIgnoreCase)) ||
            (secondPart.Equals(AcceptTypePart.AsSpan(), StringComparison.OrdinalIgnoreCase) &&
             firstPart.Trim().Equals(AcceptVersionPart.AsSpan(), StringComparison.OrdinalIgnoreCase));
    }
}
