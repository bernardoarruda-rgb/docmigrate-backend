using DocMigrate.Domain.Common;

namespace DocMigrate.Domain.Entities;

public class UserPreference : BaseEntity
{
    public int UserId { get; set; }
    public string Settings { get; set; } = "{}";
    public User User { get; set; } = null!;
}
