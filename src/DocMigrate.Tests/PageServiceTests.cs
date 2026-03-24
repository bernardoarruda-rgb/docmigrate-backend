using DocMigrate.Application.DTOs.Page;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using DocMigrate.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Page = DocMigrate.Domain.Entities.Page;

namespace DocMigrate.Tests;

public class PageServiceTests
{
    private static readonly IPlainTextExtractor StubExtractor = new StubPlainTextExtractor();
    private static readonly IPageTranslationService StubTranslationService = new StubPageTranslationService();
    private static readonly Mock<IServiceScopeFactory> StubScopeFactory = new();
    private static readonly ILogger<PageService> StubLogger = new Mock<ILogger<PageService>>().Object;

    private class StubPlainTextExtractor : IPlainTextExtractor
    {
        public string? Extract(string? json) => json;
    }

    private class StubPageTranslationService : IPageTranslationService
    {
        public Task<List<Application.DTOs.Translation.TranslationListItem>> GetTranslationsAsync(int pageId) => Task.FromResult(new List<Application.DTOs.Translation.TranslationListItem>());
        public Task<Application.DTOs.Translation.TranslationResponse> GetTranslationAsync(int pageId, string language) => throw new KeyNotFoundException();
        public Task<Application.DTOs.Translation.TranslationResponse> GenerateTranslationAsync(int pageId, string language, int? userId = null) => throw new NotImplementedException();
        public Task<Application.DTOs.Translation.TranslationResponse> UpdateTranslationAsync(int pageId, string language, Application.DTOs.Translation.UpdateTranslationRequest request, int? userId = null) => throw new NotImplementedException();
        public Task DeleteTranslationAsync(int pageId, string language) => Task.CompletedTask;
        public Task MarkOutdatedAsync(int pageId) => Task.CompletedTask;
        public Task SoftDeleteByPageAsync(int pageId) => Task.CompletedTask;
        public Task AutoTranslateAsync(int pageId, int? userId = null) => Task.CompletedTask;
    }

    private static User CreateUser(int id = 100) => new()
    {
        Id = id,
        KeycloakId = $"keycloak-{id}",
        Name = "Usuario Teste",
        Email = $"user{id}@test.com",
    };

    private static Space CreateSpace(int id = 1) => new()
    {
        Id = id,
        Title = "Espaco Teste",
    };

    private static Page CreatePage(int id, int spaceId = 1, int sortOrder = 0, string? content = null, int? parentPageId = null, int level = 1) => new()
    {
        Id = id,
        Title = $"Pagina {id}",
        Description = $"Descricao {id}",
        Content = content ?? $"Conteudo {id}",
        SortOrder = sortOrder,
        SpaceId = spaceId,
        ParentPageId = parentPageId,
        Level = level,
    };

    private static async Task SeedBaseEntities(AppDbContext context, int spaceId = 1)
    {
        context.Users.Add(CreateUser());
        context.Spaces.Add(CreateSpace(spaceId));
        await context.SaveChangesAsync();
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_SpaceHasNoPages_ReturnsEmptyList()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_SpaceHasNoPages_ReturnsEmptyList));
        await SeedBaseEntities(context);

        await using var readContext = TestDbContextFactory.Create(nameof(GetAllAsync_SpaceHasNoPages_ReturnsEmptyList));
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithPages_ReturnsPagesForSpace()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_WithPages_ReturnsPagesForSpace));
        await SeedBaseEntities(context);
        context.Spaces.Add(CreateSpace(id: 2));
        context.Pages.AddRange(
            CreatePage(1, spaceId: 1),
            CreatePage(2, spaceId: 1),
            CreatePage(3, spaceId: 2));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetAllAsync_WithPages_ReturnsPagesForSpace));
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.SpaceId == 1);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeletedPages()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_ExcludesDeletedPages));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.DeletedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetAllAsync_ExcludesDeletedPages));
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_OrdersBySortOrder()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_OrdersBySortOrder));
        await SeedBaseEntities(context);
        context.Pages.AddRange(
            CreatePage(1, sortOrder: 2),
            CreatePage(2, sortOrder: 0),
            CreatePage(3, sortOrder: 1));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetAllAsync_OrdersBySortOrder));
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().HaveCount(3);
        result[0].SortOrder.Should().Be(0);
        result[1].SortOrder.Should().Be(1);
        result[2].SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotIncludeContent()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_DoesNotIncludeContent));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: "conteudo que nao deve aparecer"));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetAllAsync_DoesNotIncludeContent));
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().ContainSingle();
        var listItem = result[0];
        listItem.Should().BeOfType<PageListItem>();
        typeof(PageListItem).GetProperty("Content").Should().BeNull();
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingPage_ReturnsPageWithContent()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_ExistingPage_ReturnsPageWithContent));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: "{\"type\":\"doc\"}"));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetByIdAsync_ExistingPage_ReturnsPageWithContent));
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Content.Should().Be("{\"type\":\"doc\"}");
        result.Title.Should().Be("Pagina 1");
        result.SpaceId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_NonExistingPage_ThrowsKeyNotFoundException));
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.GetByIdAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    [Fact]
    public async Task GetByIdAsync_DeletedPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_DeletedPage_ThrowsKeyNotFoundException));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.DeletedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetByIdAsync_DeletedPage_ThrowsKeyNotFoundException));
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.GetByIdAsync(1);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsPage()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_ValidRequest_CreatesAndReturnsPage));
        await SeedBaseEntities(context);

        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);
        var request = new CreatePageRequest
        {
            Title = "Nova Pagina",
            Description = "Descricao da nova pagina",
            Content = "{\"type\":\"doc\",\"content\":[]}",
            SortOrder = 5,
            SpaceId = 1,
        };

        var result = await service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Title.Should().Be("Nova Pagina");
        result.Description.Should().Be("Descricao da nova pagina");
        result.Content.Should().Be("{\"type\":\"doc\",\"content\":[]}");
        result.SortOrder.Should().Be(5);
        result.SpaceId.Should().Be(1);
        result.Id.Should().BeGreaterThan(0);

        await using var readContext = TestDbContextFactory.Create(nameof(CreateAsync_ValidRequest_CreatesAndReturnsPage));
        var persisted = await readContext.Pages.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Nova Pagina");
    }

    [Fact]
    public async Task CreateAsync_NonExistingSpace_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_NonExistingSpace_ThrowsKeyNotFoundException));
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);
        var request = new CreatePageRequest
        {
            Title = "Pagina Orfao",
            SpaceId = 999,
        };

        Func<Task> act = () => service.CreateAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Espaco nao encontrado");
    }

    [Fact]
    public async Task CreateAsync_DeletedSpace_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_DeletedSpace_ThrowsKeyNotFoundException));
        var space = CreateSpace();
        space.DeletedAt = DateTime.UtcNow;
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);
        var request = new CreatePageRequest
        {
            Title = "Pagina em Espaco Deletado",
            SpaceId = 1,
        };

        Func<Task> act = () => service.CreateAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Espaco nao encontrado");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingPage_UpdatesAllFields()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingPage_UpdatesAllFields));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var updateContext = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingPage_UpdatesAllFields));
        var service = new PageService(updateContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);
        var request = new UpdatePageRequest
        {
            Title = "Titulo Atualizado",
            Description = "Descricao Atualizada",
            Content = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\"}]}",
            SortOrder = 10,
        };

        var result = await service.UpdateAsync(1, request);

        result.Title.Should().Be("Titulo Atualizado");
        result.Description.Should().Be("Descricao Atualizada");
        result.Content.Should().Be("{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\"}]}");
        result.SortOrder.Should().Be(10);

        await using var readContext = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingPage_UpdatesAllFields));
        var persisted = await readContext.Pages.FindAsync(1);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Titulo Atualizado");
        persisted.Description.Should().Be("Descricao Atualizada");
        persisted.Content.Should().Be("{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\"}]}");
        persisted.SortOrder.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_NonExistingPage_ThrowsKeyNotFoundException));
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);
        var request = new UpdatePageRequest
        {
            Title = "Titulo",
        };

        Func<Task> act = () => service.UpdateAsync(999, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingPage_SoftDeletes()
    {
        await using var context = TestDbContextFactory.Create(nameof(DeleteAsync_ExistingPage_SoftDeletes));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var deleteContext = TestDbContextFactory.Create(nameof(DeleteAsync_ExistingPage_SoftDeletes));
        var service = new PageService(deleteContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        await service.DeleteAsync(1);

        await using var readContext = TestDbContextFactory.Create(nameof(DeleteAsync_ExistingPage_SoftDeletes));
        var entity = await readContext.Pages.FindAsync(1);
        entity.Should().NotBeNull();
        entity!.DeletedAt.Should().NotBeNull();
        entity.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(DeleteAsync_NonExistingPage_ThrowsKeyNotFoundException));
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.DeleteAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion

    #region AcquireLockAsync

    [Fact]
    public async Task AcquireLockAsync_UnlockedPage_ReturnsTrue()
    {
        await using var context = TestDbContextFactory.Create(nameof(AcquireLockAsync_UnlockedPage_ReturnsTrue));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var lockContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_UnlockedPage_ReturnsTrue));
        var service = new PageService(lockContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.AcquireLockAsync(1, "user-abc");

        result.Should().BeTrue();

        await using var readContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_UnlockedPage_ReturnsTrue));
        var entity = await readContext.Pages.FindAsync(1);
        entity!.LockedBy.Should().Be("user-abc");
        entity.LockedAt.Should().NotBeNull();
        entity.LockedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task AcquireLockAsync_SameUser_ReturnsTrue()
    {
        await using var context = TestDbContextFactory.Create(nameof(AcquireLockAsync_SameUser_ReturnsTrue));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.LockedBy = "user-abc";
        page.LockedAt = DateTime.UtcNow.AddMinutes(-5);
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var lockContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_SameUser_ReturnsTrue));
        var service = new PageService(lockContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.AcquireLockAsync(1, "user-abc");

        result.Should().BeTrue();

        await using var readContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_SameUser_ReturnsTrue));
        var entity = await readContext.Pages.FindAsync(1);
        entity!.LockedBy.Should().Be("user-abc");
        entity.LockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AcquireLockAsync_DifferentUserActiveLock_ReturnsFalse()
    {
        await using var context = TestDbContextFactory.Create(nameof(AcquireLockAsync_DifferentUserActiveLock_ReturnsFalse));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.LockedBy = "user-abc";
        page.LockedAt = DateTime.UtcNow.AddMinutes(-5);
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var lockContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_DifferentUserActiveLock_ReturnsFalse));
        var service = new PageService(lockContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.AcquireLockAsync(1, "user-xyz");

        result.Should().BeFalse();

        await using var readContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_DifferentUserActiveLock_ReturnsFalse));
        var entity = await readContext.Pages.FindAsync(1);
        entity!.LockedBy.Should().Be("user-abc");
    }

    [Fact]
    public async Task AcquireLockAsync_ExpiredLock_ReturnsTrue()
    {
        await using var context = TestDbContextFactory.Create(nameof(AcquireLockAsync_ExpiredLock_ReturnsTrue));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.LockedBy = "user-abc";
        page.LockedAt = DateTime.UtcNow.AddMinutes(-31);
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var lockContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_ExpiredLock_ReturnsTrue));
        var service = new PageService(lockContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.AcquireLockAsync(1, "user-xyz");

        result.Should().BeTrue();

        await using var readContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_ExpiredLock_ReturnsTrue));
        var entity = await readContext.Pages.FindAsync(1);
        entity!.LockedBy.Should().Be("user-xyz");
        entity.LockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AcquireLockAsync_NonExistentPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(AcquireLockAsync_NonExistentPage_ThrowsKeyNotFoundException));
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.AcquireLockAsync(999, "user-abc");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    [Fact]
    public async Task AcquireLockAsync_SoftDeletedPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(AcquireLockAsync_SoftDeletedPage_ThrowsKeyNotFoundException));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.DeletedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var lockContext = TestDbContextFactory.Create(nameof(AcquireLockAsync_SoftDeletedPage_ThrowsKeyNotFoundException));
        var service = new PageService(lockContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.AcquireLockAsync(1, "user-abc");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion

    #region ReleaseLockAsync

    [Fact]
    public async Task ReleaseLockAsync_LockedBySameUser_ReturnsTrue()
    {
        await using var context = TestDbContextFactory.Create(nameof(ReleaseLockAsync_LockedBySameUser_ReturnsTrue));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.LockedBy = "user-abc";
        page.LockedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var releaseContext = TestDbContextFactory.Create(nameof(ReleaseLockAsync_LockedBySameUser_ReturnsTrue));
        var service = new PageService(releaseContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.ReleaseLockAsync(1, "user-abc");

        result.Should().BeTrue();

        await using var readContext = TestDbContextFactory.Create(nameof(ReleaseLockAsync_LockedBySameUser_ReturnsTrue));
        var entity = await readContext.Pages.FindAsync(1);
        entity!.LockedBy.Should().BeNull();
        entity.LockedAt.Should().BeNull();
    }

    [Fact]
    public async Task ReleaseLockAsync_LockedByDifferentUser_ReturnsFalse()
    {
        await using var context = TestDbContextFactory.Create(nameof(ReleaseLockAsync_LockedByDifferentUser_ReturnsFalse));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.LockedBy = "user-abc";
        page.LockedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var releaseContext = TestDbContextFactory.Create(nameof(ReleaseLockAsync_LockedByDifferentUser_ReturnsFalse));
        var service = new PageService(releaseContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.ReleaseLockAsync(1, "user-xyz");

        result.Should().BeFalse();

        await using var readContext = TestDbContextFactory.Create(nameof(ReleaseLockAsync_LockedByDifferentUser_ReturnsFalse));
        var entity = await readContext.Pages.FindAsync(1);
        entity!.LockedBy.Should().Be("user-abc");
    }

    [Fact]
    public async Task ReleaseLockAsync_UnlockedPage_ReturnsFalse()
    {
        await using var context = TestDbContextFactory.Create(nameof(ReleaseLockAsync_UnlockedPage_ReturnsFalse));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var releaseContext = TestDbContextFactory.Create(nameof(ReleaseLockAsync_UnlockedPage_ReturnsFalse));
        var service = new PageService(releaseContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.ReleaseLockAsync(1, "user-abc");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseLockAsync_NonExistentPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(ReleaseLockAsync_NonExistentPage_ThrowsKeyNotFoundException));
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.ReleaseLockAsync(999, "user-abc");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion

    #region AutosaveContentAsync

    [Fact]
    public async Task AutosaveContentAsync_LockedBySameUser_UpdatesContentAndRenewsLock()
    {
        await using var context = TestDbContextFactory.Create(nameof(AutosaveContentAsync_LockedBySameUser_UpdatesContentAndRenewsLock));
        await SeedBaseEntities(context);
        var page = CreatePage(1, content: "conteudo antigo");
        page.LockedBy = "user-abc";
        page.LockedAt = DateTime.UtcNow.AddMinutes(-10);
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var saveContext = TestDbContextFactory.Create(nameof(AutosaveContentAsync_LockedBySameUser_UpdatesContentAndRenewsLock));
        var service = new PageService(saveContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        await service.AutosaveContentAsync(1, "user-abc", "{\"type\":\"doc\",\"content\":[]}");

        await using var readContext = TestDbContextFactory.Create(nameof(AutosaveContentAsync_LockedBySameUser_UpdatesContentAndRenewsLock));
        var entity = await readContext.Pages.FindAsync(1);
        entity!.Content.Should().Be("{\"type\":\"doc\",\"content\":[]}");
        entity.LockedBy.Should().Be("user-abc");
        entity.LockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AutosaveContentAsync_LockedByDifferentUser_ThrowsInvalidOperationException()
    {
        await using var context = TestDbContextFactory.Create(nameof(AutosaveContentAsync_LockedByDifferentUser_ThrowsInvalidOperationException));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.LockedBy = "user-abc";
        page.LockedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var saveContext = TestDbContextFactory.Create(nameof(AutosaveContentAsync_LockedByDifferentUser_ThrowsInvalidOperationException));
        var service = new PageService(saveContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.AutosaveContentAsync(1, "user-xyz", "novo conteudo");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Voce nao possui o lock desta pagina.");
    }

    [Fact]
    public async Task AutosaveContentAsync_UnlockedPage_ThrowsInvalidOperationException()
    {
        await using var context = TestDbContextFactory.Create(nameof(AutosaveContentAsync_UnlockedPage_ThrowsInvalidOperationException));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var saveContext = TestDbContextFactory.Create(nameof(AutosaveContentAsync_UnlockedPage_ThrowsInvalidOperationException));
        var service = new PageService(saveContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.AutosaveContentAsync(1, "user-abc", "novo conteudo");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Voce nao possui o lock desta pagina.");
    }

    [Fact]
    public async Task AutosaveContentAsync_NonExistentPage_ThrowsKeyNotFoundException()
    {
        await using var context = TestDbContextFactory.Create(nameof(AutosaveContentAsync_NonExistentPage_ThrowsKeyNotFoundException));
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.AutosaveContentAsync(999, "user-abc", "conteudo");

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion

    #region Authorship

    [Fact]
    public async Task CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy)}");
        await SeedBaseEntities(context);

        await using var readContext = TestDbContextFactory.Create($"Page_{nameof(CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy)}");
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.CreateAsync(new CreatePageRequest
        {
            Title = "Test Page",
            SortOrder = 0,
            SpaceId = 1,
        }, userId: 100);

        var entity = await readContext.Pages.FindAsync(result.Id);
        entity!.CreatedByUserId.Should().Be(100);
        entity.UpdatedByUserId.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy)}");
        await SeedBaseEntities(context);
        context.Users.Add(CreateUser(id: 101));
        var page = CreatePage(1);
        page.CreatedByUserId = 100;
        page.UpdatedByUserId = 100;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create($"Page_{nameof(UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy)}");
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        await service.UpdateAsync(1, new UpdatePageRequest
        {
            Title = "Updated Title",
            SortOrder = 0,
        }, userId: 101);

        var entity = await readContext.Pages.FindAsync(1);
        entity!.CreatedByUserId.Should().Be(100);
        entity.UpdatedByUserId.Should().Be(101);
    }

    [Fact]
    public async Task CreateAsync_WithoutUserId_LeavesAuthorshipNull()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(CreateAsync_WithoutUserId_LeavesAuthorshipNull)}");
        await SeedBaseEntities(context);

        await using var readContext = TestDbContextFactory.Create($"Page_{nameof(CreateAsync_WithoutUserId_LeavesAuthorshipNull)}");
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.CreateAsync(new CreatePageRequest
        {
            Title = "No Author",
            SortOrder = 0,
            SpaceId = 1,
        });

        var entity = await readContext.Pages.FindAsync(result.Id);
        entity!.CreatedByUserId.Should().BeNull();
        entity.UpdatedByUserId.Should().BeNull();
    }

    #endregion

    #region GetHeadingsAsync

    [Fact]
    public async Task GetHeadingsAsync_PageWithHeadings_ReturnsHeadingsWithSlugs()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_PageWithHeadings_ReturnsHeadingsWithSlugs)}");
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: "{\"type\":\"doc\",\"content\":[{\"type\":\"heading\",\"attrs\":{\"level\":2},\"content\":[{\"type\":\"text\",\"text\":\"Introdução\"}]},{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Texto qualquer\"}]},{\"type\":\"heading\",\"attrs\":{\"level\":3},\"content\":[{\"type\":\"text\",\"text\":\"Configuração\"}]}]}"));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_PageWithHeadings_ReturnsHeadingsWithSlugs)}");
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetHeadingsAsync(1);

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("introducao");
        result[0].Text.Should().Be("Introdução");
        result[0].Level.Should().Be(2);
        result[1].Id.Should().Be("configuracao");
        result[1].Text.Should().Be("Configuração");
        result[1].Level.Should().Be(3);
    }

    [Fact]
    public async Task GetHeadingsAsync_DuplicateHeadings_DeduplicatesSlugs()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_DuplicateHeadings_DeduplicatesSlugs)}");
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: "{\"type\":\"doc\",\"content\":[{\"type\":\"heading\",\"attrs\":{\"level\":2},\"content\":[{\"type\":\"text\",\"text\":\"Setup\"}]},{\"type\":\"heading\",\"attrs\":{\"level\":2},\"content\":[{\"type\":\"text\",\"text\":\"Setup\"}]},{\"type\":\"heading\",\"attrs\":{\"level\":2},\"content\":[{\"type\":\"text\",\"text\":\"Setup\"}]}]}"));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_DuplicateHeadings_DeduplicatesSlugs)}");
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetHeadingsAsync(1);

        result.Should().HaveCount(3);
        result[0].Id.Should().Be("setup");
        result[1].Id.Should().Be("setup-1");
        result[2].Id.Should().Be("setup-2");
    }

    [Fact]
    public async Task GetHeadingsAsync_NoContent_ReturnsEmptyList()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_NoContent_ReturnsEmptyList)}");
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: null));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_NoContent_ReturnsEmptyList)}");
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetHeadingsAsync(1);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHeadingsAsync_NonExistentPage_ThrowsKeyNotFound()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_NonExistentPage_ThrowsKeyNotFound)}");
        var service = new PageService(context, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        Func<Task> act = () => service.GetHeadingsAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetHeadingsAsync_MalformedJson_ReturnsEmptyList()
    {
        await using var context = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_MalformedJson_ReturnsEmptyList)}");
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: "not valid json {{{"));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create($"Page_{nameof(GetHeadingsAsync_MalformedJson_ReturnsEmptyList)}");
        var service = new PageService(readContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.GetHeadingsAsync(1);

        result.Should().BeEmpty();
    }

    #endregion

    #region Hierarchy

    [Fact]
    public async Task CreateAsync_WithValidParent_SetsLevelCorrectly()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_WithValidParent_SetsLevelCorrectly));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(CreateAsync_WithValidParent_SetsLevelCorrectly));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var result = await service.CreateAsync(new CreatePageRequest
        {
            Title = "Sub-pagina", SpaceId = 1, SortOrder = 0, ParentPageId = 1,
        });

        result.Level.Should().Be(2);
        result.ParentPageId.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ExceedsMaxDepth_Throws()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_ExceedsMaxDepth_Throws));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, level: 5));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(CreateAsync_ExceedsMaxDepth_Throws));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var act = () => service.CreateAsync(new CreatePageRequest
        {
            Title = "Nivel 6", SpaceId = 1, SortOrder = 0, ParentPageId = 1,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*5 niveis*");
    }

    [Fact]
    public async Task CreateAsync_ParentInDifferentSpace_Throws()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_ParentInDifferentSpace_Throws));
        await SeedBaseEntities(context);
        context.Spaces.Add(CreateSpace(2));
        context.Pages.Add(CreatePage(1, spaceId: 2));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(CreateAsync_ParentInDifferentSpace_Throws));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var act = () => service.CreateAsync(new CreatePageRequest
        {
            Title = "Wrong space", SpaceId = 1, SortOrder = 0, ParentPageId = 1,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*mesmo espaco*");
    }

    [Fact]
    public async Task DeleteAsync_WithChildren_CascadeSoftDeletes()
    {
        await using var context = TestDbContextFactory.Create(nameof(DeleteAsync_WithChildren_CascadeSoftDeletes));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        context.Pages.Add(CreatePage(2, parentPageId: 1, level: 2));
        context.Pages.Add(CreatePage(3, parentPageId: 2, level: 3));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(DeleteAsync_WithChildren_CascadeSoftDeletes));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        await service.DeleteAsync(1);

        await using var checkContext = TestDbContextFactory.Create(nameof(DeleteAsync_WithChildren_CascadeSoftDeletes));
        var allPages = await checkContext.Pages.IgnoreQueryFilters().ToListAsync();
        allPages.Should().AllSatisfy(p => p.DeletedAt.Should().NotBeNull());
    }

    [Fact]
    public async Task UpdateAsync_MoveToDescendant_Throws()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_MoveToDescendant_Throws));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        context.Pages.Add(CreatePage(2, parentPageId: 1, level: 2));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(UpdateAsync_MoveToDescendant_Throws));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var act = () => service.UpdateAsync(1, new UpdatePageRequest
        {
            Title = "Pagina 1", SortOrder = 0, ParentPageId = 2,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ciclo*");
    }

    [Fact]
    public async Task UpdateAsync_SelfParent_Throws()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_SelfParent_Throws));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(UpdateAsync_SelfParent_Throws));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var act = () => service.UpdateAsync(1, new UpdatePageRequest
        {
            Title = "Pagina 1", SortOrder = 0, ParentPageId = 1,
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pai de si mesma*");
    }

    [Fact]
    public async Task UpdateAsync_MoveToNewParent_RecalculatesDescendantLevels()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_MoveToNewParent_RecalculatesDescendantLevels));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        context.Pages.Add(CreatePage(2, parentPageId: 1, level: 2));
        context.Pages.Add(CreatePage(3, parentPageId: 2, level: 3));
        context.Pages.Add(CreatePage(4));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(UpdateAsync_MoveToNewParent_RecalculatesDescendantLevels));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        await service.UpdateAsync(2, new UpdatePageRequest
        {
            Title = "Pagina 2", SortOrder = 0, ParentPageId = 4,
        });

        await using var checkContext = TestDbContextFactory.Create(nameof(UpdateAsync_MoveToNewParent_RecalculatesDescendantLevels));
        var page2 = await checkContext.Pages.FindAsync(2);
        var page3 = await checkContext.Pages.FindAsync(3);
        page2!.Level.Should().Be(2);
        page3!.Level.Should().Be(3);
        page2.ParentPageId.Should().Be(4);
    }

    [Fact]
    public async Task GetBreadcrumbsAsync_ReturnsPathFromRootToPage()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetBreadcrumbsAsync_ReturnsPathFromRootToPage));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        context.Pages.Add(CreatePage(2, parentPageId: 1, level: 2));
        context.Pages.Add(CreatePage(3, parentPageId: 2, level: 3));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(GetBreadcrumbsAsync_ReturnsPathFromRootToPage));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var breadcrumbs = await service.GetBreadcrumbsAsync(3);

        breadcrumbs.Should().HaveCount(3);
        breadcrumbs[0].Id.Should().Be(1);
        breadcrumbs[1].Id.Should().Be(2);
        breadcrumbs[2].Id.Should().Be(3);
    }

    [Fact]
    public async Task GetAllAsync_IncludesHierarchyFields()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_IncludesHierarchyFields));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        context.Pages.Add(CreatePage(2, parentPageId: 1, level: 2));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(GetAllAsync_IncludesHierarchyFields));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var pages = await service.GetAllAsync(1);

        var parent = pages.First(p => p.Id == 1);
        parent.ParentPageId.Should().BeNull();
        parent.Level.Should().Be(1);
        parent.HasChildren.Should().BeTrue();

        var child = pages.First(p => p.Id == 2);
        child.ParentPageId.Should().Be(1);
        child.Level.Should().Be(2);
        child.HasChildren.Should().BeFalse();
    }

    [Fact]
    public async Task ReorderAsync_CrossParentPages_Throws()
    {
        await using var context = TestDbContextFactory.Create(nameof(ReorderAsync_CrossParentPages_Throws));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        context.Pages.Add(CreatePage(2));
        context.Pages.Add(CreatePage(3, parentPageId: 1, level: 2));
        await context.SaveChangesAsync();

        await using var svcContext = TestDbContextFactory.Create(nameof(ReorderAsync_CrossParentPages_Throws));
        var service = new PageService(svcContext, StubExtractor, StubTranslationService, StubScopeFactory.Object, StubLogger);

        var act = () => service.ReorderAsync(1, new ReorderPagesRequest
        {
            Items = [new() { PageId = 2, SortOrder = 1 }, new() { PageId = 3, SortOrder = 2 }],
        });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*mesmo pai*");
    }

    #endregion
}
