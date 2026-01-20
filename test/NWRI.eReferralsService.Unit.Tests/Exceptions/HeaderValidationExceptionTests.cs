using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Errors;
using WCCG.eReferralsService.API.Exceptions;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Exceptions;

public class HeaderValidationExceptionTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateHeaderValidationException()
    {
        //Arrange
        var missingRequiredHeaderValidationFailures = _fixture.Build<ValidationFailure>()
            .With(x => x.ErrorCode, ValidationErrorCode.MissingRequiredHeaderCode.ToString)
            .CreateMany(2).ToList();

        var invalidHeaderValidationFailures = _fixture.Build<ValidationFailure>()
            .With(x => x.ErrorCode, ValidationErrorCode.InvalidHeaderCode.ToString)
            .CreateMany(3).ToList();

        List<ValidationFailure> validationFailures = [.. invalidHeaderValidationFailures, .. missingRequiredHeaderValidationFailures];
        var expectedMessage = $"Header(s) validation failure: {string.Join(';', validationFailures.Select(x => x.ErrorMessage))}";

        //Act
        var exception = new HeaderValidationException(validationFailures);

        //Assert
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.OfType<MissingRequiredHeaderError>().Should().HaveCount(2);
        exception.Errors.OfType<InvalidHeaderError>().Should().HaveCount(3);
    }
}
