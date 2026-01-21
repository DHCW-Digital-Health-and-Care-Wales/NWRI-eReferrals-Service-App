using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.Unit.Tests.Extensions;

namespace NWRI.eReferralsService.Unit.Tests.Exceptions;

public class BundleValidationExceptionTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldCorrectlyCreateBundleValidationException()
    {
        //Arrange

        var validationFailures = _fixture.CreateMany<ValidationFailure>().ToList();
        var expectedMessage = $"Bundle validation failure: {string.Join(';', validationFailures.Select(x => x.ErrorMessage))}.";

        //Act
        var exception = new BundleValidationException(validationFailures);

        //Assert
        exception.Message.Should().Be(expectedMessage);
        exception.Errors.Should().AllSatisfy(e => e.Should().BeOfType<InvalidBundleError>());
    }
}


