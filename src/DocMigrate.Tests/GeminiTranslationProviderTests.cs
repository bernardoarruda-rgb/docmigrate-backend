using System.Net;
using System.Text;
using System.Text.Json;
using DocMigrate.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;

namespace DocMigrate.Tests;

public class GeminiTranslationProviderTests
{
    private static GeminiTranslationProvider CreateProvider(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("GeminiTranslation")).Returns(new HttpClient(handler));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GeminiTranslation:ApiKey"] = "test-key",
                ["GeminiTranslation:Model"] = "gemini-2.5-flash",
            })
            .Build();

        return new GeminiTranslationProvider(factory.Object, config);
    }

    private static HttpMessageHandler MockHandler(HttpStatusCode status, string responseBody)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            });
        return mock.Object;
    }

    [Fact]
    public async Task TranslateTextAsync_Success_ReturnsTranslatedText()
    {
        var response = JsonSerializer.Serialize(new
        {
            candidates = new[] { new { content = new { parts = new[] { new { text = "The deploy was done via pipeline" } } } } }
        });
        var provider = CreateProvider(MockHandler(HttpStatusCode.OK, response));

        var result = await provider.TranslateTextAsync("O deploy foi feito via pipeline", "pt-BR", "en");

        result.Success.Should().BeTrue();
        result.TranslatedText.Should().Be("The deploy was done via pipeline");
    }

    [Fact]
    public async Task TranslateTextAsync_ApiError_ReturnsFailure()
    {
        var provider = CreateProvider(MockHandler(HttpStatusCode.InternalServerError, "{}"));

        var result = await provider.TranslateTextAsync("Texto", "pt-BR", "en");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TranslateTextAsync_SendsApiKeyInHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text = "translated" } } } } } }),
                    Encoding.UTF8, "application/json"),
            });

        var provider = CreateProvider(mock.Object);
        await provider.TranslateTextAsync("test", "pt-BR", "en");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.GetValues("x-goog-api-key").Should().Contain("test-key");
    }

    [Fact]
    public async Task TranslateTextAsync_UsesCorrectEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text = "translated" } } } } } }),
                    Encoding.UTF8, "application/json"),
            });

        var provider = CreateProvider(mock.Object);
        await provider.TranslateTextAsync("test", "pt-BR", "en");

        capturedRequest!.RequestUri!.ToString().Should().Contain("generativelanguage.googleapis.com");
        capturedRequest.RequestUri.ToString().Should().Contain("gemini-2.5-flash");
    }

    [Theory]
    [InlineData("pt-BR", "Brazilian Portuguese")]
    [InlineData("en", "English")]
    [InlineData("es", "Spanish")]
    public async Task TranslateTextAsync_MapsLanguageCodesToNames(string langCode, string expectedName)
    {
        string? capturedBody = null;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
                capturedBody = await req.Content!.ReadAsStringAsync())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text = "translated" } } } } } }),
                    Encoding.UTF8, "application/json"),
            });

        var provider = CreateProvider(mock.Object);
        await provider.TranslateTextAsync("test", langCode, "en");

        capturedBody.Should().Contain(expectedName);
    }
}
