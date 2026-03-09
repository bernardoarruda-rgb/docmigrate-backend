using DocMigrate.Application.DTOs.Space;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DocMigrate.Tests;

public class SpaceServiceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WithSpaces_ReturnsAllActiveSpaces()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllAsync_WithSpaces_ReturnsAllActiveSpaces));
        context.Spaces.AddRange(
            new Space { Title = "Alpha", Description = "First space" },
            new Space { Title = "Beta", Description = "Second space" }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeletedSpaces()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllAsync_ExcludesDeletedSpaces));
        context.Spaces.AddRange(
            new Space { Title = "Active Space" },
            new Space { Title = "Deleted Space", DeletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Active Space");
    }

    [Fact]
    public async Task GetAllAsync_OrdersByTitle()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllAsync_OrdersByTitle));
        context.Spaces.AddRange(
            new Space { Title = "Charlie" },
            new Space { Title = "Alpha" },
            new Space { Title = "Bravo" }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Alpha");
        result[1].Title.Should().Be("Bravo");
        result[2].Title.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetAllAsync_IncludesPageCount()
    {
        // Arrange
        using var context = CreateContext(nameof(GetAllAsync_IncludesPageCount));
        var space = new Space { Title = "Space with pages" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        context.Pages.AddRange(
            new Page { Title = "Active Page 1", SpaceId = space.Id, SortOrder = 1 },
            new Page { Title = "Active Page 2", SpaceId = space.Id, SortOrder = 2 },
            new Page { Title = "Deleted Page", SpaceId = space.Id, SortOrder = 3, DeletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].PageCount.Should().Be(2);
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingSpace_ReturnsSpace()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_ExistingSpace_ReturnsSpace));
        var space = new Space { Title = "Test Space", Description = "A test description" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetByIdAsync(space.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(space.Id);
        result.Title.Should().Be("Test Space");
        result.Description.Should().Be("A test description");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingSpace_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_NonExistingSpace_ThrowsKeyNotFoundException));
        var service = new SpaceService(context);

        // Act
        var act = () => service.GetByIdAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedSpace_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateContext(nameof(GetByIdAsync_DeletedSpace_ThrowsKeyNotFoundException));
        var space = new Space { Title = "Deleted Space", DeletedAt = DateTime.UtcNow };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var act = () => service.GetByIdAsync(space.Id);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsSpace()
    {
        // Arrange
        using var context = CreateContext(nameof(CreateAsync_ValidRequest_CreatesAndReturnsSpace));
        var service = new SpaceService(context);
        var request = new CreateSpaceRequest { Title = "New Space", Description = "New description" };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Space");
        result.Description.Should().Be("New description");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var persisted = await context.Spaces.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("New Space");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingSpace_UpdatesAndReturnsSpace()
    {
        // Arrange
        using var context = CreateContext(nameof(UpdateAsync_ExistingSpace_UpdatesAndReturnsSpace));
        var space = new Space { Title = "Original Title", Description = "Original description" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);
        var request = new UpdateSpaceRequest { Title = "Updated Title", Description = "Updated description" };

        // Act
        var result = await service.UpdateAsync(space.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated description");

        var persisted = await context.Spaces.FindAsync(space.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Updated Title");
        persisted.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateAsync_NonExistingSpace_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateContext(nameof(UpdateAsync_NonExistingSpace_ThrowsKeyNotFoundException));
        var service = new SpaceService(context);
        var request = new UpdateSpaceRequest { Title = "Any Title" };

        // Act
        var act = () => service.UpdateAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingSpace_SoftDeletes()
    {
        // Arrange
        using var context = CreateContext(nameof(DeleteAsync_ExistingSpace_SoftDeletes));
        var space = new Space { Title = "Space to delete" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        await service.DeleteAsync(space.Id);

        // Assert
        var persisted = await context.Spaces.FindAsync(space.Id);
        persisted.Should().NotBeNull();
        persisted!.DeletedAt.Should().NotBeNull();
        persisted.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_SpaceWithPages_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateContext(nameof(DeleteAsync_SpaceWithPages_ThrowsInvalidOperationException));
        var space = new Space { Title = "Space with pages" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        context.Pages.Add(new Page { Title = "Active Page", SpaceId = space.Id, SortOrder = 1 });
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var act = () => service.DeleteAsync(space.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*paginas vinculadas*");
    }

    [Fact]
    public async Task DeleteAsync_NonExistingSpace_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = CreateContext(nameof(DeleteAsync_NonExistingSpace_ThrowsKeyNotFoundException));
        var service = new SpaceService(context);

        // Act
        var act = () => service.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    #endregion
}
