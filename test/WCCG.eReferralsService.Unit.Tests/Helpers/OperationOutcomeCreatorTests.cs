using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using Hl7.Fhir.Model;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.API.Exceptions;
using WCCG.eReferralsService.API.Helpers;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Helpers;

public class OperationOutcomeCreatorTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void CreateOperationOutcomeShouldCreateFromErrors()
    {
        //Arrange
        var errors = _fixture.CreateMany<MissingRequiredHeaderError>().ToArray<BaseFhirHttpError>();

        var expectedIssues = errors.Select(error => new OperationOutcome.IssueComponent
        {
            Severity = OperationOutcome.IssueSeverity.Error,
            Code = error.IssueType,
            Details = new CodeableConcept(BaseFhirHttpError.System, error.Code, error.Display),
            Diagnostics = error.DiagnosticsMessage
        }).ToList();

        //Act
        var result = OperationOutcomeCreator.CreateOperationOutcome(errors);

        //Assert
        result.Id.Should().NotBeEmpty();
        result.Meta.Should().NotBeNull();
        result.Meta!.Profile.Should().BeEquivalentTo(new List<string> { FhirConstants.OperationOutcomeProfile });
        result.Issue.Should().BeEquivalentTo(expectedIssues);
    }

    [Fact]
    public void CreateOperationOutcomeShouldCreateFromFhirException()
    {
        //Arrange
        var validationFailures = _fixture.Build<ValidationFailure>()
            .With(x => x.ErrorCode, ValidationErrorCode.MissingRequiredHeaderCode.ToString)
            .CreateMany().ToList();
        var exception = new HeaderValidationException(validationFailures);

        var expectedIssues = exception.Errors.Select(error => new OperationOutcome.IssueComponent
        {
            Severity = OperationOutcome.IssueSeverity.Error,
            Code = error.IssueType,
            Details = new CodeableConcept(BaseFhirHttpError.System, error.Code, error.Display),
            Diagnostics = error.DiagnosticsMessage
        }).ToList();

        //Act
        var result = OperationOutcomeCreator.CreateOperationOutcome(exception);

        //Assert
        result.Id.Should().NotBeEmpty();
        result.Meta.Should().NotBeNull();
        result.Meta!.Profile.Should().BeEquivalentTo(new List<string> { FhirConstants.OperationOutcomeProfile });
        result.Issue.Should().BeEquivalentTo(expectedIssues);
    }
}
