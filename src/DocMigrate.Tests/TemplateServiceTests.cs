using DocMigrate.Application.DTOs.Template;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using DocMigrate.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DocMigrate.Tests;

public class TemplateServiceTests
{
    private static User CreateUser(int id = 100) => new()
    {
        Id = id,
        KeycloakId = $"keycloak-{id}",
        Name = "Usuario Teste",
        Email = $"user{id}@test.com",
    };

    private static Template CreateTemplate(
        int id,
        string? title = null,
        string? description = null,
        string? icon = null,
        string? content = null,
        bool isDefault = false,
        int sortOrder = 0) => new()
    {
        Id = id,
        Title = title ?? $"Template {id}",
        Description = description ?? $"Descricao {id}",
        Icon = icon ?? "file-text",
        Content = content,
        IsDefault = isDefault,
        SortOrder = sortOrder,
    };

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_NoTemplates_ReturnsEmptyList()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_NoTemplates_ReturnsEmptyList));
        var service = new TemplateService(context);

        var result = await service.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_HasTemplates_ReturnsOrderedBySortOrderThenTitle()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_HasTemplates_ReturnsOrderedBySortOrderThenTitle));
        context.Templates.AddRange(
            CreateTemplate(1, title: "Charlie", sortOrder: 1),
            CreateTemplate(2, title: "Alpha", sortOrder: 0),
            CreateTemplate(3, title: "Bravo", sortOrder: 1));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetAllAsync_HasTemplates_ReturnsOrderedBySortOrderThenTitle));
        var service = new TemplateService(readContext);

        var result = await service.GetAllAsync();

        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Alpha");
        result[1].Title.Should().Be("Bravo");
        result[2].Title.Should().Be("Charlie");
    }

    [Fact]
    public async Task GetAllAsync_SoftDeletedTemplates_ExcludesDeleted()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetAllAsync_SoftDeletedTemplates_ExcludesDeleted));
        var active = CreateTemplate(1, title: "Ativo");
        var deleted = CreateTemplate(2, title: "Deletado");
        deleted.DeletedAt = DateTime.UtcNow;
        context.Templates.AddRange(active, deleted);
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetAllAsync_SoftDeletedTemplates_ExcludesDeleted));
        var service = new TemplateService(readContext);

        var result = await service.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Ativo");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingTemplate_ReturnsResponse()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_ExistingTemplate_ReturnsResponse));
        context.Templates.Add(CreateTemplate(1, title: "Meu Template", description: "Desc", icon: "star", content: "{\"type\":\"doc\"}", sortOrder: 3));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetByIdAsync_ExistingTemplate_ReturnsResponse));
        var service = new TemplateService(readContext);

        var result = await service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Title.Should().Be("Meu Template");
        result.Description.Should().Be("Desc");
        result.Icon.Should().Be("star");
        result.Content.Should().Be("{\"type\":\"doc\"}");
        result.SortOrder.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentTemplate_ThrowsKeyNotFound()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_NonExistentTemplate_ThrowsKeyNotFound));
        var service = new TemplateService(context);

        Func<Task> act = () => service.GetByIdAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template nao encontrado");
    }

    [Fact]
    public async Task GetByIdAsync_SoftDeletedTemplate_ThrowsKeyNotFound()
    {
        await using var context = TestDbContextFactory.Create(nameof(GetByIdAsync_SoftDeletedTemplate_ThrowsKeyNotFound));
        var template = CreateTemplate(1);
        template.DeletedAt = DateTime.UtcNow;
        context.Templates.Add(template);
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(GetByIdAsync_SoftDeletedTemplate_ThrowsKeyNotFound));
        var service = new TemplateService(readContext);

        Func<Task> act = () => service.GetByIdAsync(1);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template nao encontrado");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsResponseWithId()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_ValidRequest_ReturnsResponseWithId));
        var service = new TemplateService(context);
        var request = new CreateTemplateRequest
        {
            Title = "Novo Template",
            Description = "Descricao do template",
            Icon = "layout",
            SortOrder = 5,
        };

        var result = await service.CreateAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Novo Template");
        result.Description.Should().Be("Descricao do template");
        result.Icon.Should().Be("layout");
        result.SortOrder.Should().Be(5);

        await using var readContext = TestDbContextFactory.Create(nameof(CreateAsync_ValidRequest_ReturnsResponseWithId));
        var persisted = await readContext.Templates.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Novo Template");
    }

    [Fact]
    public async Task CreateAsync_WithContent_PersistsContent()
    {
        await using var context = TestDbContextFactory.Create(nameof(CreateAsync_WithContent_PersistsContent));
        var service = new TemplateService(context);
        var request = new CreateTemplateRequest
        {
            Title = "Template com Conteudo",
            Content = "{\"type\":\"doc\",\"content\":[{\"type\":\"heading\"}]}",
            SortOrder = 0,
        };

        var result = await service.CreateAsync(request);

        result.Content.Should().Be("{\"type\":\"doc\",\"content\":[{\"type\":\"heading\"}]}");

        await using var readContext = TestDbContextFactory.Create(nameof(CreateAsync_WithContent_PersistsContent));
        var persisted = await readContext.Templates.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Content.Should().Be("{\"type\":\"doc\",\"content\":[{\"type\":\"heading\"}]}");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingTemplate_UpdatesFields()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingTemplate_UpdatesFields));
        context.Templates.Add(CreateTemplate(1, title: "Original", description: "Desc original", icon: "file", sortOrder: 0));
        await context.SaveChangesAsync();

        await using var updateContext = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingTemplate_UpdatesFields));
        var service = new TemplateService(updateContext);
        var request = new UpdateTemplateRequest
        {
            Title = "Atualizado",
            Description = "Desc atualizada",
            Icon = "star",
            Content = "{\"type\":\"doc\",\"content\":[]}",
            SortOrder = 10,
        };

        var result = await service.UpdateAsync(1, request);

        result.Title.Should().Be("Atualizado");
        result.Description.Should().Be("Desc atualizada");
        result.Icon.Should().Be("star");
        result.Content.Should().Be("{\"type\":\"doc\",\"content\":[]}");
        result.SortOrder.Should().Be(10);

        await using var readContext = TestDbContextFactory.Create(nameof(UpdateAsync_ExistingTemplate_UpdatesFields));
        var persisted = await readContext.Templates.FindAsync(1);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Atualizado");
        persisted.Description.Should().Be("Desc atualizada");
        persisted.Icon.Should().Be("star");
        persisted.Content.Should().Be("{\"type\":\"doc\",\"content\":[]}");
        persisted.SortOrder.Should().Be(10);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentTemplate_ThrowsKeyNotFound()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_NonExistentTemplate_ThrowsKeyNotFound));
        var service = new TemplateService(context);
        var request = new UpdateTemplateRequest
        {
            Title = "Qualquer",
        };

        Func<Task> act = () => service.UpdateAsync(999, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template nao encontrado");
    }

    [Fact]
    public async Task UpdateAsync_SoftDeletedTemplate_ThrowsKeyNotFound()
    {
        await using var context = TestDbContextFactory.Create(nameof(UpdateAsync_SoftDeletedTemplate_ThrowsKeyNotFound));
        var template = CreateTemplate(1);
        template.DeletedAt = DateTime.UtcNow;
        context.Templates.Add(template);
        await context.SaveChangesAsync();

        await using var updateContext = TestDbContextFactory.Create(nameof(UpdateAsync_SoftDeletedTemplate_ThrowsKeyNotFound));
        var service = new TemplateService(updateContext);
        var request = new UpdateTemplateRequest
        {
            Title = "Tentativa",
        };

        Func<Task> act = () => service.UpdateAsync(1, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template nao encontrado");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingTemplate_SoftDeletes()
    {
        await using var context = TestDbContextFactory.Create(nameof(DeleteAsync_ExistingTemplate_SoftDeletes));
        context.Templates.Add(CreateTemplate(1));
        await context.SaveChangesAsync();

        await using var deleteContext = TestDbContextFactory.Create(nameof(DeleteAsync_ExistingTemplate_SoftDeletes));
        var service = new TemplateService(deleteContext);

        await service.DeleteAsync(1);

        await using var readContext = TestDbContextFactory.Create(nameof(DeleteAsync_ExistingTemplate_SoftDeletes));
        var entity = await readContext.Templates.FindAsync(1);
        entity.Should().NotBeNull();
        entity!.DeletedAt.Should().NotBeNull();
        entity.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentTemplate_ThrowsKeyNotFound()
    {
        await using var context = TestDbContextFactory.Create(nameof(DeleteAsync_NonExistentTemplate_ThrowsKeyNotFound));
        var service = new TemplateService(context);

        Func<Task> act = () => service.DeleteAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template nao encontrado");
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedTemplate_ThrowsKeyNotFound()
    {
        await using var context = TestDbContextFactory.Create(nameof(DeleteAsync_AlreadyDeletedTemplate_ThrowsKeyNotFound));
        var template = CreateTemplate(1);
        template.DeletedAt = DateTime.UtcNow;
        context.Templates.Add(template);
        await context.SaveChangesAsync();

        await using var deleteContext = TestDbContextFactory.Create(nameof(DeleteAsync_AlreadyDeletedTemplate_ThrowsKeyNotFound));
        var service = new TemplateService(deleteContext);

        Func<Task> act = () => service.DeleteAsync(1);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Template nao encontrado");
    }

    #endregion

    #region Authorship

    [Fact]
    public async Task CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy()
    {
        await using var context = TestDbContextFactory.Create($"Template_{nameof(CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy)}");
        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(CreateAsync_WithUserId_SetsCreatedByAndUpdatedBy));
        var service = new TemplateService(readContext);

        var result = await service.CreateAsync(new CreateTemplateRequest
        {
            Title = "Test Template",
            SortOrder = 0,
        }, userId: 100);

        var entity = await readContext.Templates.FindAsync(result.Id);
        entity!.CreatedByUserId.Should().Be(100);
        entity.UpdatedByUserId.Should().Be(100);
    }

    [Fact]
    public async Task UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy()
    {
        await using var context = TestDbContextFactory.Create($"Template_{nameof(UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy)}");
        context.Users.Add(CreateUser());
        context.Users.Add(CreateUser(id: 101));
        var template = CreateTemplate(1);
        template.CreatedByUserId = 100;
        template.UpdatedByUserId = 100;
        context.Templates.Add(template);
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create($"Template_{nameof(UpdateAsync_WithUserId_OnlyUpdatesUpdatedBy)}");
        var service = new TemplateService(readContext);

        await service.UpdateAsync(1, new UpdateTemplateRequest
        {
            Title = "Updated",
            SortOrder = 0,
        }, userId: 101);

        var entity = await readContext.Templates.FindAsync(1);
        entity!.CreatedByUserId.Should().Be(100);
        entity.UpdatedByUserId.Should().Be(101);
    }

    #endregion
}
