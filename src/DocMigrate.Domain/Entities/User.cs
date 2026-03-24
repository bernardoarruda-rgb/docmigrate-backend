using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class User : BaseEntity
{
    public string KeycloakId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "admin";
    public UserPreference? Preference { get; set; }
}
