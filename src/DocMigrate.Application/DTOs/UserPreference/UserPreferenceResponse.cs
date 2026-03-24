namespace DocMigrate.Application.DTOs.UserPreference;

public class UserPreferenceResponse
{
    public int UserId { get; set; }
    public UserSettings Settings { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
