using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using WCCG.eReferralsService.API.Constants;
using WCCG.eReferralsService.API.Models;
using WCCG.eReferralsService.Unit.Tests.Extensions;

namespace WCCG.eReferralsService.Unit.Tests.Models;

public class HeadersModelTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();

    [Fact]
    public void ShouldSetAllProperties()
    {
        //Arrange
        var headers = new HeaderDictionary();

        foreach (var header in RequestHeaderKeys.GetAll())
        {
            headers.Add(header, _fixture.Create<string>());
        }

        //Act
        var model = HeadersModel.FromHeaderDictionary(headers);

        //Assert
        model.TargetIdentifier.Should().NotBeNull();
        model.EndUserOrganisation.Should().NotBeNull();
        model.RequestingSoftware.Should().NotBeNull();
        model.RequestId.Should().NotBeNull();
        model.RequestingPractitioner.Should().NotBeNull();
        model.CorrelationId.Should().NotBeNull();
        model.UseContext.Should().NotBeNull();
        model.Accept.Should().NotBeNull();
    }

    [Fact]
    public void ShouldSetNullIfNotExists()
    {
        //Arrange
        var headers = _fixture.Create<HeaderDictionary>();

        //Act
        var model = HeadersModel.FromHeaderDictionary(headers);

        //Assert
        model.TargetIdentifier.Should().BeNull();
        model.EndUserOrganisation.Should().BeNull();
        model.RequestingSoftware.Should().BeNull();
        model.RequestId.Should().BeNull();
        model.RequestingPractitioner.Should().BeNull();
        model.CorrelationId.Should().BeNull();
        model.UseContext.Should().BeNull();
        model.Accept.Should().BeNull();
    }
}
