using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Models.WPAS.Requests;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using RichardSzalay.MockHttp;

namespace NWRI.eReferralsService.Unit.Tests.Services;

public sealed class WpasApiClientTests
{
    private readonly IFixture _fixture = new Fixture().WithCustomizations();
    private readonly WpasApiConfig _wpasApiConfig;

    public WpasApiClientTests()
    {
        _wpasApiConfig = _fixture.Build<WpasApiConfig>()
            .With(x => x.BaseUrl, "https://some.com")
            .With(x => x.CreateReferralEndpoint, _fixture.Create<string>())
            .With(x => x.CancelReferralEndpoint, _fixture.Create<string>())
            .With(x => x.GetReferralEndpoint, $"{_fixture.Create<string>()}/{{0}}")
            .Create();

        _fixture.Mock<IOptions<WpasApiConfig>>().SetupGet(x => x.Value).Returns(_wpasApiConfig);
    }

    [Fact]
    public async Task CreateReferralAsyncShouldPostJsonWithJsonMediaType()
    {
        // Arrange
        var request = CreateWpasCreateReferralRequest();
        var expectedRequestJson = JsonSerializer.Serialize(request);
        var expectedReferralId = "140:12345678";
        var expectedResponseJson = $@"{{""System"":""Welsh PAS"",""ReferralId"":""{expectedReferralId}""}}";

        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CreateReferralEndpoint}")
            .WithContent(expectedRequestJson)
            .WithHeaders(HeaderNames.ContentType, MediaTypeNames.Application.Json)
            .Respond(MediaTypeNames.Application.Json, expectedResponseJson);

        using var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

        var logger = Mock.Of<ILogger<WpasApiClient>>();
        var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, logger);

        // Act
        var result = await sut.CreateReferralAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ReferralId.Should().Be(expectedReferralId);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task CancelReferralAsyncShouldPostJsonWithJsonMediaType()
    {
        // Arrange
        var requestBody = new WpasCancelReferralRequest();
        var expectedRequestJson = JsonSerializer.Serialize(requestBody);
        var expectedReferralId = "140:12345678";
        var expectedResponseJson = $@"{{""ReferralId"":""{expectedReferralId}""}}";

        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CancelReferralEndpoint}")
            .WithContent(expectedRequestJson)
            .WithHeaders(HeaderNames.ContentType, MediaTypeNames.Application.Json)
            .Respond(MediaTypeNames.Application.Json, expectedResponseJson);

        using var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

        var logger = Mock.Of<ILogger<WpasApiClient>>();
        var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, logger);

        // Act
        var result = await sut.CancelReferralAsync(requestBody, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ReferralId.Should().Be(expectedReferralId);

        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task CreateReferralAsyncShouldThrowWhenNonSuccessWithProblemDetails(HttpStatusCode statusCode)
    {
        // Arrange
        var requestBody = CreateWpasCreateReferralRequest();
        var problemDetails = _fixture.Create<ProblemDetails>();

        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CreateReferralEndpoint}")
            .Respond(statusCode, JsonContent.Create(problemDetails));

        using var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

        var logger = Mock.Of<ILogger<WpasApiClient>>();
        var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, logger);

        // Act
        var action = async () => await sut.CreateReferralAsync(requestBody, CancellationToken.None);

        // Assert
        var exception = (await action.Should().ThrowAsync<NotSuccessfulApiCallException>()).Subject.ToList();
        exception[0].StatusCode.Should().Be(statusCode);
        exception[0].Errors.Should().AllSatisfy(e => e.Should().BeOfType<NotSuccessfulApiResponseError>());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task CreateReferralAsyncShouldThrowWhenNonJsonContent(HttpStatusCode statusCode)
    {
        // Arrange
        var requestBody = CreateWpasCreateReferralRequest();
        var rawContent = _fixture.Create<string>();

        using var mockHttp = new MockHttpMessageHandler();
        mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CreateReferralEndpoint}")
            .Respond(statusCode, new StringContent(rawContent));

        using var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

        var logger = Mock.Of<ILogger<WpasApiClient>>();
        var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, logger);

        // Act
        var action = async () => await sut.CreateReferralAsync(requestBody, CancellationToken.None);

        // Assert
        var exception = (await action.Should().ThrowAsync<NotSuccessfulApiCallException>()).Subject.ToList();
        exception[0].StatusCode.Should().Be(statusCode);
        exception[0].Errors.Should().AllSatisfy(e => e.Should().BeOfType<UnexpectedError>());
    }

    private static WpasCreateReferralRequest CreateWpasCreateReferralRequest()
    {
        return new WpasCreateReferralRequest
        {
            RecordId = "77220d53-3fd2-41d1-b8b3-878e6771ef75",
            ContractDetails = new ContractDetails
            {
                ProviderOrganisationCode = "TP2VC"
            },
            PatientDetails = new PatientDetails
            {
                NhsNumber = "3478526985",
                NhsNumberStatusIndicator = "01",
                PatientName = new PatientName
                {
                    Surname = "Jones",
                    FirstName = "Julie"
                },
                BirthDate = "19590504",
                Sex = "F",
                UsualAddress = new UsualAddress
                {
                    NoAndStreet = "22 Brightside Crescent",
                    Town = "Overtown",
                    Postcode = "LS10 4YU",
                    Locality = ""
                }
            },
            ReferralDetails = new ReferralDetails
            {
                OutpatientReferralSource = "15",
                ReferringOrganisationCode = "TP2VC",
                ServiceTypeRequested = "6",
                ReferrerCode = "01-99999",
                AdministrativeCategory = "01",
                DateOfReferral = "20240820",
                MainSpecialty = "130",
                ReferrerPriorityType = "2",
                ReasonForReferral = "glau-sre",
                ReferralIdentifier = "140:12345678"
            }
        };
    }
}
