using System.Net.Http.Json;
using System.Text.Json;
using DocMigrate.Application.DTOs.Unsplash;
using DocMigrate.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocMigrate.Infrastructure.Services;

public class UnsplashService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<UnsplashService> logger) : IUnsplashService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<UnsplashSearchResponse> SearchAsync(string query, int page = 1, int perPage = 12)
    {
        var accessKey = configuration["Unsplash:AccessKey"];
        if (string.IsNullOrEmpty(accessKey))
            return new UnsplashSearchResponse();

        var client = httpClientFactory.CreateClient("Unsplash");
        var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(query)}&page={page}&per_page={perPage}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Client-ID {accessKey}");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Unsplash API returned {StatusCode} for query '{Query}'", (int)response.StatusCode, query);
            return new UnsplashSearchResponse();
        }

        var json = await response.Content.ReadFromJsonAsync<UnsplashApiResponse>(JsonOptions);
        if (json is null) return new UnsplashSearchResponse();

        return new UnsplashSearchResponse
        {
            TotalPages = json.TotalPages,
            Results = json.Results.Select(r => new UnsplashPhoto
            {
                Id = r.Id,
                Urls = new UnsplashPhotoUrls
                {
                    Regular = r.Urls.Regular,
                    Small = r.Urls.Small,
                },
                User = new UnsplashUser
                {
                    Name = r.User.Name,
                    Link = r.User.Links.Html,
                },
            }).ToList(),
        };
    }

    private class UnsplashApiResponse
    {
        public int TotalPages { get; set; }
        public List<UnsplashApiPhoto> Results { get; set; } = [];
    }

    private class UnsplashApiPhoto
    {
        public string Id { get; set; } = string.Empty;
        public UnsplashApiUrls Urls { get; set; } = new();
        public UnsplashApiUser User { get; set; } = new();
    }

    private class UnsplashApiUrls
    {
        public string Regular { get; set; } = string.Empty;
        public string Small { get; set; } = string.Empty;
    }

    private class UnsplashApiUser
    {
        public string Name { get; set; } = string.Empty;
        public UnsplashApiLinks Links { get; set; } = new();
    }

    private class UnsplashApiLinks
    {
        public string Html { get; set; } = string.Empty;
    }
}
