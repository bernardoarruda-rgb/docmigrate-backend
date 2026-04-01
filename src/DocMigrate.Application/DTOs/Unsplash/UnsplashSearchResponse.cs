namespace DocMigrate.Application.DTOs.Unsplash;

public class UnsplashSearchResponse
{
    public List<UnsplashPhoto> Results { get; set; } = [];
    public int TotalPages { get; set; }
}

public class UnsplashPhoto
{
    public string Id { get; set; } = string.Empty;
    public UnsplashPhotoUrls Urls { get; set; } = new();
    public UnsplashUser User { get; set; } = new();
}

public class UnsplashPhotoUrls
{
    public string Regular { get; set; } = string.Empty;
    public string Small { get; set; } = string.Empty;
}

public class UnsplashUser
{
    public string Name { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
}
