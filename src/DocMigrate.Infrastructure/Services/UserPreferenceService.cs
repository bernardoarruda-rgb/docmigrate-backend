using System.Text.Json;
using DocMigrate.Application.DTOs.UserPreference;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Infrastructure.Services;

public class UserPreferenceService(AppDbContext context) : IUserPreferenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<UserPreferenceResponse> GetByUserIdAsync(int userId)
    {
        await EnsureUserExistsAsync(userId);

        var preference = await context.UserPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (preference is null)
        {
            return new UserPreferenceResponse
            {
                UserId = userId,
                Settings = new UserSettings(),
                UpdatedAt = DateTime.UtcNow,
            };
        }

        return MapToResponse(preference);
    }

    public async Task<UserPreferenceResponse> UpdateAsync(int userId, UpdateUserPreferenceRequest request)
    {
        await EnsureUserExistsAsync(userId);

        var preference = await context.UserPreferences
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (preference is null)
        {
            preference = new UserPreference
            {
                UserId = userId,
                Settings = "{}",
            };
            context.UserPreferences.Add(preference);
        }

        var existingSettings = JsonSerializer.Deserialize<UserSettings>(preference.Settings, JsonOptions) ?? new UserSettings();
        var mergedSettings = MergeSettings(existingSettings, request);
        preference.Settings = JsonSerializer.Serialize(mergedSettings, JsonOptions);

        await context.SaveChangesAsync();

        return MapToResponse(preference);
    }

    public async Task ResetAsync(int userId)
    {
        await EnsureUserExistsAsync(userId);

        var preference = await context.UserPreferences
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .FirstOrDefaultAsync();

        if (preference is not null)
        {
            preference.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private async Task EnsureUserExistsAsync(int userId)
    {
        var userExists = await context.Users
            .AnyAsync(u => u.Id == userId && u.DeletedAt == null);

        if (!userExists)
            throw new KeyNotFoundException("Usuario nao encontrado.");
    }

    private static UserSettings MergeSettings(UserSettings existing, UpdateUserPreferenceRequest request)
    {
        if (request.ThemePalette != null) existing.ThemePalette = request.ThemePalette;
        if (request.CustomPrimaryColor != null) existing.CustomPrimaryColor = request.CustomPrimaryColor;
        if (request.ColorMode != null) existing.ColorMode = request.ColorMode;
        if (request.HeadingFont != null) existing.HeadingFont = request.HeadingFont;
        if (request.BodyFont != null) existing.BodyFont = request.BodyFont;
        if (request.BaseFontSize != null) existing.BaseFontSize = request.BaseFontSize;
        if (request.LineHeight != null) existing.LineHeight = request.LineHeight;
        if (request.ContentWidth != null) existing.ContentWidth = request.ContentWidth;
        if (request.BlockSpacing != null) existing.BlockSpacing = request.BlockSpacing;
        if (request.SidebarDefault != null) existing.SidebarDefault = request.SidebarDefault;
        if (request.HighContrast != null) existing.HighContrast = request.HighContrast;
        if (request.FontSizeMultiplier != null) existing.FontSizeMultiplier = request.FontSizeMultiplier;
        if (request.ReducedAnimations != null) existing.ReducedAnimations = request.ReducedAnimations;

        return existing;
    }

    private UserPreferenceResponse MapToResponse(UserPreference entity)
    {
        var settings = JsonSerializer.Deserialize<UserSettings>(entity.Settings, JsonOptions) ?? new UserSettings();

        return new UserPreferenceResponse
        {
            UserId = entity.UserId,
            Settings = settings,
            UpdatedAt = entity.UpdatedAt,
        };
    }
}
