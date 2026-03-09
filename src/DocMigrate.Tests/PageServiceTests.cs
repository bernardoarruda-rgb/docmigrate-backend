using DocMigrate.Application.DTOs.Page;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Page = DocMigrate.Domain.Entities.Page;

namespace DocMigrate.Tests;

public class PageServiceTests
{
    private static AppDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static Space CreateSpace(int id = 1) => new()
    {
        Id = id,
        Title = "Espaco Teste",
    };

    private static Page CreatePage(int id, int spaceId = 1, int sortOrder = 0, string? content = null) => new()
    {
        Id = id,
        Title = $"Pagina {id}",
        Description = $"Descricao {id}",
        Content = content ?? $"Conteudo {id}",
        SortOrder = sortOrder,
        SpaceId = spaceId,
    };

    private static async Task SeedBaseEntities(AppDbContext context, int spaceId = 1)
    {
        context.Spaces.Add(CreateSpace(spaceId));
        await context.SaveChangesAsync();
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_WithPages_ReturnsPagesForSpace()
    {
        await using var context = CreateContext(nameof(GetAllAsync_WithPages_ReturnsPagesForSpace));
        await SeedBaseEntities(context);
        context.Spaces.Add(CreateSpace(id: 2));
        context.Pages.AddRange(
            CreatePage(1, spaceId: 1),
            CreatePage(2, spaceId: 1),
            CreatePage(3, spaceId: 2));
        await context.SaveChangesAsync();

        await using var readContext = CreateContext(nameof(GetAllAsync_WithPages_ReturnsPagesForSpace));
        var service = new PageService(readContext);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.SpaceId == 1);
    }

    [Fact]
    public async Task GetAllAsync_ExcludesDeletedPages()
    {
        await using var context = CreateContext(nameof(GetAllAsync_ExcludesDeletedPages));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.DeletedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var readContext = CreateContext(nameof(GetAllAsync_ExcludesDeletedPages));
        var service = new PageService(readContext);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_OrdersBySortOrder()
    {
        await using var context = CreateContext(nameof(GetAllAsync_OrdersBySortOrder));
        await SeedBaseEntities(context);
        context.Pages.AddRange(
            CreatePage(1, sortOrder: 2),
            CreatePage(2, sortOrder: 0),
            CreatePage(3, sortOrder: 1));
        await context.SaveChangesAsync();

        await using var readContext = CreateContext(nameof(GetAllAsync_OrdersBySortOrder));
        var service = new PageService(readContext);

        var result = await service.GetAllAsync(spaceId: 1);

        result.Should().HaveCount(3);
        result[0].SortOrder.Should().Be(0);
        result[1].SortOrder.Should().Be(1);
        result[2].SortOrder.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_DoesNotIncludeContent()
    {
        await using var context = CreateContext(nameof(GetAllAsync_DoesNotIncludeContent));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: "conteudo que nao deve aparecer"));
        await context.SaveChangesAsync();

        await using var readContext = CreateContext(nameof(GetAllAsync_DoesNotIncludeContent));
        var service = new PageService(readContext);

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
        await using var context = CreateContext(nameof(GetByIdAsync_ExistingPage_ReturnsPageWithContent));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1, content: "{\"type\":\"doc\"}"));
        await context.SaveChangesAsync();

        await using var readContext = CreateContext(nameof(GetByIdAsync_ExistingPage_ReturnsPageWithContent));
        var service = new PageService(readContext);

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
        await using var context = CreateContext(nameof(GetByIdAsync_NonExistingPage_ThrowsKeyNotFoundException));
        var service = new PageService(context);

        Func<Task> act = () => service.GetByIdAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    [Fact]
    public async Task GetByIdAsync_DeletedPage_ThrowsKeyNotFoundException()
    {
        await using var context = CreateContext(nameof(GetByIdAsync_DeletedPage_ThrowsKeyNotFoundException));
        await SeedBaseEntities(context);
        var page = CreatePage(1);
        page.DeletedAt = DateTime.UtcNow;
        context.Pages.Add(page);
        await context.SaveChangesAsync();

        await using var readContext = CreateContext(nameof(GetByIdAsync_DeletedPage_ThrowsKeyNotFoundException));
        var service = new PageService(readContext);

        Func<Task> act = () => service.GetByIdAsync(1);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesAndReturnsPage()
    {
        await using var context = CreateContext(nameof(CreateAsync_ValidRequest_CreatesAndReturnsPage));
        await SeedBaseEntities(context);

        var service = new PageService(context);
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

        await using var readContext = CreateContext(nameof(CreateAsync_ValidRequest_CreatesAndReturnsPage));
        var persisted = await readContext.Pages.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Nova Pagina");
    }

    [Fact]
    public async Task CreateAsync_NonExistingSpace_ThrowsKeyNotFoundException()
    {
        await using var context = CreateContext(nameof(CreateAsync_NonExistingSpace_ThrowsKeyNotFoundException));
        var service = new PageService(context);
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
        await using var context = CreateContext(nameof(CreateAsync_DeletedSpace_ThrowsKeyNotFoundException));
        var space = CreateSpace();
        space.DeletedAt = DateTime.UtcNow;
        context.Spaces.Add(space);
        await context.SaveChangesAsync();

        var service = new PageService(context);
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
        await using var context = CreateContext(nameof(UpdateAsync_ExistingPage_UpdatesAllFields));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var updateContext = CreateContext(nameof(UpdateAsync_ExistingPage_UpdatesAllFields));
        var service = new PageService(updateContext);
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

        await using var readContext = CreateContext(nameof(UpdateAsync_ExistingPage_UpdatesAllFields));
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
        await using var context = CreateContext(nameof(UpdateAsync_NonExistingPage_ThrowsKeyNotFoundException));
        var service = new PageService(context);
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
        await using var context = CreateContext(nameof(DeleteAsync_ExistingPage_SoftDeletes));
        await SeedBaseEntities(context);
        context.Pages.Add(CreatePage(1));
        await context.SaveChangesAsync();

        await using var deleteContext = CreateContext(nameof(DeleteAsync_ExistingPage_SoftDeletes));
        var service = new PageService(deleteContext);

        await service.DeleteAsync(1);

        await using var readContext = CreateContext(nameof(DeleteAsync_ExistingPage_SoftDeletes));
        var entity = await readContext.Pages.FindAsync(1);
        entity.Should().NotBeNull();
        entity!.DeletedAt.Should().NotBeNull();
        entity.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingPage_ThrowsKeyNotFoundException()
    {
        await using var context = CreateContext(nameof(DeleteAsync_NonExistingPage_ThrowsKeyNotFoundException));
        var service = new PageService(context);

        Func<Task> act = () => service.DeleteAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Pagina nao encontrada");
    }

    #endregion
}
