using AutoFixture;
using FluentAssertions;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Helpers;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Helpers;

public class FhirHttpErrorHelperTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Theory]
    [InlineData(FhirHttpErrorCodes.SenderBadRequest, "400: The API was unable to process the request.")]
    [InlineData(FhirHttpErrorCodes.ReceiverServerError, "500: The Receiver has encountered an error processing the request.")]
    [InlineData(FhirHttpErrorCodes.ReceiverBadRequest, "400: The Receiver was unable to process the request.")]
    [InlineData(FhirHttpErrorCodes.ReceiverUnavailable, "503: The Receiver is currently unavailable.")]
    [InlineData(FhirHttpErrorCodes.TooManyRequests, "429: Too many requests have been made by this source in a given amount of time.")]
    [InlineData(FhirHttpErrorCodes.ReceiverNotFound, "404: The Receiver was unable to find the specified resource.")]
    public void GetDisplayMessageByCodeShouldReturnCorrectMessages(string code, string expectedMessage)
    {
        //Act
        var message = FhirHttpErrorHelper.GetDisplayMessageByCode(code);

        //Assert
        message.Should().Be(expectedMessage);
    }

    [Fact]
    public void GetDisplayMessageByCodeShouldReturnEmptyWhenCodeNotFound()
    {
        //Arrange
        var notValidCode = _fixture.Create<string>();

        //Act
        var message = FhirHttpErrorHelper.GetDisplayMessageByCode(notValidCode);

        //Assert
        message.Should().BeEmpty();
    }
}
