namespace DocMigrate.Application.DTOs.UserPreference;

public class UpdateUserPreferenceRequest
{
    public string? ThemePalette { get; set; }
    public string? CustomPrimaryColor { get; set; }
    public string? ColorMode { get; set; }
    public string? HeadingFont { get; set; }
    public string? BodyFont { get; set; }
    public int? BaseFontSize { get; set; }
    public decimal? LineHeight { get; set; }
    public string? ContentWidth { get; set; }
    public string? BlockSpacing { get; set; }
    public string? SidebarDefault { get; set; }
    public bool? HighContrast { get; set; }
    public decimal? FontSizeMultiplier { get; set; }
    public bool? ReducedAnimations { get; set; }
}
