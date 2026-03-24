using DocMigrate.Application.DTOs.Space;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using DocMigrate.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DocMigrate.Tests;

public class SpaceServiceTests
{
    private static User CreateUser(int id = 100) => new()
    {
        Id = id,
        KeycloakId = $"keycloak-{id}",
        Name = "Usuario Teste",
        Email = $"user{id}@test.com",
    };

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_EmptyDatabase_ReturnsEmptyList));
        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithSpaces_ReturnsAllActiveSpaces()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_WithSpaces_ReturnsAllActiveSpaces));
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
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_ExcludesDeletedSpaces));
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
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_OrdersByTitle));
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
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_IncludesPageCount));
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
        using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_ExistingSpace_ReturnsSpace));
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
        using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_NonExistingSpace_ThrowsKeyNotFoundException));
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
        using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_DeletedSpace_ThrowsKeyNotFoundException));
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
        using var context = TestDbContextFactory.Create(nameof(CreateAsync_ValidRequest_CreatesAndReturnsSpace));
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
        using var context = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingSpace_UpdatesAndReturnsSpace));
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
        using var context = TestDbContextFactory.Create(nameof(UpdateAsync_NonExistingSpace_ThrowsKeyNotFoundException));
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
        using var context = TestDbContextFactory.Create(nameof(DeleteAsync_ExistingSpace_SoftDeletes));
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
        using var context = TestDbContextFactory.Create(nameof(DeleteAsync_SpaceWithPages_ThrowsInvalidOperationException));
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
        using var context = TestDbContextFactory.Create(nameof(DeleteAsync_NonExistingSpace_ThrowsKeyNotFoundException));
        var service = new SpaceService(context);

        // Act
        var act = () => service.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedSpace_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(DeleteAsync_AlreadyDeletedSpace_ThrowsKeyNotFoundException));
        var space = new Space { Title = "Already Deleted", DeletedAt = DateTime.UtcNow };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var act = () => service.DeleteAsync(space.Id);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_SpaceWithOnlyDeletedPages_Succeeds()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(DeleteAsync_SpaceWithOnlyDeletedPages_Succeeds));
        var space = new Space { Title = "Space with deleted pages" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        context.Pages.Add(new Page { Title = "Deleted Page", SpaceId = space.Id, SortOrder = 1, DeletedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        await service.DeleteAsync(space.Id);

        // Assert
        var persisted = await context.Spaces.FindAsync(space.Id);
        persisted.Should().NotBeNull();
        persisted!.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_WithUtcKind()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(DeleteAsync_SoftDeletes_WithUtcKind));
        var space = new Space { Title = "UTC Check" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        await service.DeleteAsync(space.Id);

        // Assert
        var persisted = await context.Spaces.FindAsync(space.Id);
        persisted.Should().NotBeNull();
        persisted!.DeletedAt.Should().NotBeNull();
        persisted.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region UpdateAsync (additional)

    [Fact]
    public async Task UpdateAsync_SoftDeletedSpace_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(UpdateAsync_SoftDeletedSpace_ThrowsKeyNotFoundException));
        var space = new Space { Title = "Deleted Space", DeletedAt = DateTime.UtcNow };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);
        var request = new UpdateSpaceRequest { Title = "Attempt" };

        // Act
        var act = () => service.UpdateAsync(space.Id, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_AllFields_PersistsAllFields()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(UpdateAsync_AllFields_PersistsAllFields));
        var space = new Space { Title = "Original" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new SpaceService(context);
        var request = new UpdateSpaceRequest
        {
            Title = "Updated Title",
            Description = "Updated description",
            Icon = "rocket",
            IconColor = "#FF0000",
            BackgroundColor = "#00FF00",
        };

        // Act
        var result = await service.UpdateAsync(space.Id, request);

        // Assert
        result.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated description");
        result.Icon.Should().Be("rocket");
        result.IconColor.Should().Be("#FF0000");
        result.BackgroundColor.Should().Be("#00FF00");

        var persisted = await context.Spaces.FindAsync(space.Id);
        persisted!.Icon.Should().Be("rocket");
        persisted.IconColor.Should().Be("#FF0000");
        persisted.BackgroundColor.Should().Be("#00FF00");
    }

    #endregion

    #region CreateAsync (additional)

    [Fact]
    public async Task CreateAsync_WithAllFields_PersistsAllFields()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(CreateAsync_WithAllFields_PersistsAllFields));
        var service = new SpaceService(context);
        var request = new CreateSpaceRequest
        {
            Title = "Full Space",
            Description = "Full description",
            Icon = "globe",
            IconColor = "#AABBCC",
            BackgroundColor = "#112233",
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Title.Should().Be("Full Space");
        result.Description.Should().Be("Full description");
        result.Icon.Should().Be("globe");
        result.IconColor.Should().Be("#AABBCC");
        result.BackgroundColor.Should().Be("#112233");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GetAllAsync (paginated)

    [Fact]
    public async Task GetAllAsync_Paginated_ReturnsCorrectPage()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_Paginated_ReturnsCorrectPage));
        context.Spaces.AddRange(
            new Space { Title = "Alpha" },
            new Space { Title = "Bravo" },
            new Space { Title = "Charlie" },
            new Space { Title = "Delta" },
            new Space { Title = "Echo" }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync(page: 2, pageSize: 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
        result.Items[0].Title.Should().Be("Charlie");
        result.Items[1].Title.Should().Be("Delta");
    }

    [Fact]
    public async Task GetAllAsync_Paginated_FirstPage_HasNoPreviousPage()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_Paginated_FirstPage_HasNoPreviousPage));
        context.Spaces.AddRange(
            new Space { Title = "Alpha" },
            new Space { Title = "Bravo" },
            new Space { Title = "Charlie" }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        result.Items.Should().HaveCount(2);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_Paginated_LastPage_HasNoNextPage()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_Paginated_LastPage_HasNoNextPage));
        context.Spaces.AddRange(
            new Space { Title = "Alpha" },
            new Space { Title = "Bravo" },
            new Space { Title = "Charlie" }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync(page: 2, pageSize: 2);

        // Assert
        result.Items.Should().HaveCount(1);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_Paginated_ExcludesDeletedSpaces()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_Paginated_ExcludesDeletedSpaces));
        context.Spaces.AddRange(
            new Space { Title = "Active" },
            new Space { Title = "Deleted", DeletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync(page: 1, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Active");
    }

    [Fact]
    public async Task GetAllAsync_Paginated_EmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_Paginated_EmptyDatabase_ReturnsEmptyResult));
        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync(page: 1, pageSize: 10);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_Paginated_IncludesPageCount()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(GetAllAsync_Paginated_IncludesPageCount));
        var space = new Space { Title = "Space with pages" };
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        context.Pages.AddRange(
            new Page { Title = "Page 1", SpaceId = space.Id, SortOrder = 1 },
            new Page { Title = "Page 2", SpaceId = space.Id, SortOrder = 2 },
            new Page { Title = "Deleted Page", SpaceId = space.Id, SortOrder = 3, DeletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new SpaceService(context);

        // Act
        var result = await service.GetAllAsync(page: 1, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].PageCount.Should().Be(2);
    }

    #endregion

    #region Authorship

    [Fact]
    public async Task CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy()
    {
        await using var context = TestDbContextFactory.Create($"Space_{nameof(CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy)}");
        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();
        var service = new SpaceService(context);

        var result = await service.CreateAsync(new CreateSpaceRequest
        {
            Title = "Test Space",
        }, userId: 100);

        var entity = await context.Spaces.FindAsync(result.Id);
        entity!.CreatedByUserId.Should().Be(100);
        entity.UpdatedByUserId.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy()
    {
        await using var context = TestDbContextFactory.Create($"Space_{nameof(UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy)}");
        context.Users.Add(CreateUser());
        context.Users.Add(CreateUser(id: 101));
        context.Spaces.Add(new Space { Id = 1, Title = "S1", CreatedByUserId = 100, UpdatedByUserId = 100 });
        await context.SaveChangesAsync();
        var service = new SpaceService(context);

        await service.UpdateAsync(1, new UpdateSpaceRequest { Title = "Updated" }, userId: 101);

        var entity = await context.Spaces.FindAsync(1);
        entity!.CreatedByUserId.Should().Be(100);
        entity.UpdatedByUserId.Should().Be(101);
    }

    #endregion
}
