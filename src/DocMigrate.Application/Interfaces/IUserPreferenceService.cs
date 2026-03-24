using DocMigrate.Application.DTOs.UserPreference;

namespace DocMigrate.Application.Interfaces;

public interface IUserPreferenceService
{
    Task<UserPreferenceResponse> GetByUserIdAsync(int userId);
    Task<UserPreferenceResponse> UpdateAsync(int userId, UpdateUserPreferenceRequest request);
    Task ResetAsync(int userId);
}
