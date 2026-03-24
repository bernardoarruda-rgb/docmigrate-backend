using DocMigrate.Application.DTOs.UserPreference;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using DocMigrate.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DocMigrate.Tests;

public class UserPreferenceServiceTests
{
    private static User CreateUser(int id = 1) => new()
    {
        Id = id,
        KeycloakId = $"keycloak-{id}",
        Name = "Test User",
        Email = $"user{id}@test.com",
        Role = "admin",
    };

    private static async Task SeedUserAsync(AppDbContext context, int userId = 1)
    {
        context.Users.Add(CreateUser(userId));
        await context.SaveChangesAsync();
    }

    #region GetByUserIdAsync

    [Fact]
    public async Task GetByUserIdAsync_ExistingPreference_ReturnsPreference()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetByUserIdAsync_ExistingPreference_ReturnsPreference));
        await SeedUserAsync(context);
        context.UserPreferences.Add(new UserPreference
        {
            UserId = 1,
            Settings = """{"themePalette":"dark","colorMode":"dark"}""",
        });
        await context.SaveChangesAsync();

        var service = new UserPreferenceService(context);

        // Act
        var result = await service.GetByUserIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Settings.ThemePalette.Should().Be("dark");
        result.Settings.ColorMode.Should().Be("dark");
    }

    [Fact]
    public async Task GetByUserIdAsync_NoPreferenceExists_ReturnsDefaultResponse()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetByUserIdAsync_NoPreferenceExists_ReturnsDefaultResponse));
        await SeedUserAsync(context);

        var service = new UserPreferenceService(context);

        // Act
        var result = await service.GetByUserIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Settings.Should().NotBeNull();
        result.Settings.ThemePalette.Should().BeNull();
        result.Settings.ColorMode.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetByUserIdAsync_NonExistingUser_ThrowsKeyNotFoundException));
        var service = new UserPreferenceService(context);

        // Act
        var act = () => service.GetByUserIdAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*nao encontrado*");
    }

    [Fact]
    public async Task GetByUserIdAsync_SoftDeletedPreference_ReturnsDefault()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetByUserIdAsync_SoftDeletedPreference_ReturnsDefault));
        await SeedUserAsync(context);
        context.UserPreferences.Add(new UserPreference
        {
            UserId = 1,
            Settings = """{"themePalette":"bms"}""",
            DeletedAt = DateTime.UtcNow,
        });
        await context.SaveChangesAsync();

        var service = new UserPreferenceService(context);

        // Act
        var result = await service.GetByUserIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Settings.ThemePalette.Should().BeNull();
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_NoExistingPreference_CreatesNewPreference()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(UpdateAsync_NoExistingPreference_CreatesNewPreference));
        await SeedUserAsync(context);

        var service = new UserPreferenceService(context);
        var request = new UpdateUserPreferenceRequest
        {
            ThemePalette = "bms",
            ColorMode = "light",
        };

        // Act
        var result = await service.UpdateAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(1);
        result.Settings.ThemePalette.Should().Be("bms");
        result.Settings.ColorMode.Should().Be("light");

        var persisted = await context.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == 1 && p.DeletedAt == null);
        persisted.Should().NotBeNull();
        persisted!.Settings.Should().Contain("bms");
    }

    [Fact]
    public async Task UpdateAsync_ExistingPreference_MergesSettings()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingPreference_MergesSettings));
        await SeedUserAsync(context);
        context.UserPreferences.Add(new UserPreference
        {
            UserId = 1,
            Settings = """{"themePalette":"bms","colorMode":"light"}""",
        });
        await context.SaveChangesAsync();

        var service = new UserPreferenceService(context);
        var request = new UpdateUserPreferenceRequest
        {
            ColorMode = "dark",
        };

        // Act
        var result = await service.UpdateAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        result.Settings.ThemePalette.Should().Be("bms");
        result.Settings.ColorMode.Should().Be("dark");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(UpdateAsync_NonExistingUser_ThrowsKeyNotFoundException));
        var service = new UserPreferenceService(context);
        var request = new UpdateUserPreferenceRequest { ThemePalette = "bms" };

        // Act
        var act = () => service.UpdateAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*nao encontrado*");
    }

    #endregion

    #region ResetAsync

    [Fact]
    public async Task ResetAsync_ExistingPreference_SoftDeletesPreference()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ResetAsync_ExistingPreference_SoftDeletesPreference));
        await SeedUserAsync(context);
        context.UserPreferences.Add(new UserPreference
        {
            UserId = 1,
            Settings = """{"themePalette":"bms"}""",
        });
        await context.SaveChangesAsync();

        var service = new UserPreferenceService(context);

        // Act
        await service.ResetAsync(1);

        // Assert
        var persisted = await context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == 1);
        persisted.Should().NotBeNull();
        persisted!.DeletedAt.Should().NotBeNull();
        persisted.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ResetAsync_NoPreferenceExists_CompletesWithoutError()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ResetAsync_NoPreferenceExists_CompletesWithoutError));
        await SeedUserAsync(context);

        var service = new UserPreferenceService(context);

        // Act
        var act = () => service.ResetAsync(1);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResetAsync_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ResetAsync_NonExistingUser_ThrowsKeyNotFoundException));
        var service = new UserPreferenceService(context);

        // Act
        var act = () => service.ResetAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*nao encontrado*");
    }

    #endregion
}
