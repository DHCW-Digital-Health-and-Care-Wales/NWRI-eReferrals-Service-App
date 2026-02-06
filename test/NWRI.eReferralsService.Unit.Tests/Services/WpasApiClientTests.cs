using System.Net;
using System.Net.Http.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;
using NWRI.eReferralsService.API.Configuration;
using NWRI.eReferralsService.API.Constants;
using NWRI.eReferralsService.API.Errors;
using NWRI.eReferralsService.API.EventLogging;
using NWRI.eReferralsService.API.EventLogging.Interfaces;
using NWRI.eReferralsService.API.Exceptions;
using NWRI.eReferralsService.API.Services;
using NWRI.eReferralsService.Unit.Tests.Extensions;
using RichardSzalay.MockHttp;

namespace NWRI.eReferralsService.Unit.Tests.Services
{
    public class WpasApiClientTests
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
        public async Task CreateReferralAsyncShouldPostJsonWithFhirMediaType()
        {
            // Arrange
            var requestBody = _fixture.Create<string>();
            var expectedResponse = _fixture.Create<string>();

            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CreateReferralEndpoint}")
                .WithContent(requestBody)
                .WithHeaders(HeaderNames.ContentType, FhirConstants.FhirMediaType)
                .Respond(FhirConstants.FhirMediaType, expectedResponse);

            using var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

            var eventLogger = new Mock<IEventLogger>();
            var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, eventLogger.Object);

            // Act
            var result = await sut.CreateReferralAsync(requestBody, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);

            eventLogger.Verify(
                x => x.Audit(It.Is<EventCatalogue.DataSuccessfullyCommittedToWpas>(e => e.ExecutionTimeMs >= 0 && e.WpasReferralId == null)),
                Times.Once);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Fact]
        public async Task CancelReferralAsyncShouldPostJsonWithFhirMediaType()
        {
            // Arrange
            var requestBody = _fixture.Create<string>();
            var expectedResponse = _fixture.Create<string>();

            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CancelReferralEndpoint}")
                .WithContent(requestBody)
                .WithHeaders(HeaderNames.ContentType, FhirConstants.FhirMediaType)
                .Respond(FhirConstants.FhirMediaType, expectedResponse);

            using var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

            var eventLogger = new Mock<IEventLogger>();
            var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, eventLogger.Object);

            // Act
            var result = await sut.CancelReferralAsync(requestBody, CancellationToken.None);

            // Assert
            result.Should().Be(expectedResponse);

            eventLogger.Verify(
                x => x.Audit(It.Is<EventCatalogue.DataSuccessfullyCommittedToWpas>(e => e.ExecutionTimeMs >= 0 && e.WpasReferralId == null)),
                Times.Once);

            mockHttp.VerifyNoOutstandingExpectation();
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.NotFound)]
        public async Task CreateReferralAsyncShouldThrowWhenNonSuccessWithProblemDetails(HttpStatusCode statusCode)
        {
            // Arrange
            var requestBody = _fixture.Create<string>();
            var problemDetails = _fixture.Create<ProblemDetails>();

            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CreateReferralEndpoint}")
                .Respond(statusCode, JsonContent.Create(problemDetails));

            using var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

            var eventLogger = new Mock<IEventLogger>();
            var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, eventLogger.Object);

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
            var requestBody = _fixture.Create<string>();
            var rawContent = _fixture.Create<string>();

            using var mockHttp = new MockHttpMessageHandler();
            mockHttp.Expect(HttpMethod.Post, $"/{_wpasApiConfig.CreateReferralEndpoint}")
                .Respond(statusCode, new StringContent(rawContent));

            using var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new Uri(_wpasApiConfig.BaseUrl);

            var eventLogger = new Mock<IEventLogger>();
            var sut = new WpasApiClient(httpClient, _fixture.Mock<IOptions<WpasApiConfig>>().Object, eventLogger.Object);

            // Act
            var action = async () => await sut.CreateReferralAsync(requestBody, CancellationToken.None);

            // Assert
            var exception = (await action.Should().ThrowAsync<NotSuccessfulApiCallException>()).Subject.ToList();
            exception[0].StatusCode.Should().Be(statusCode);
            exception[0].Errors.Should().AllSatisfy(e => e.Should().BeOfType<UnexpectedError>());
        }
    }
}
