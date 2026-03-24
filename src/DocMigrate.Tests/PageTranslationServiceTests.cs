using DocMigrate.Application.DTOs.Translation;
using DocMigrate.Application.Interfaces;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using DocMigrate.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocMigrate.Tests;

public class PageTranslationServiceTests
{
    private static readonly IPlainTextExtractor StubExtractor = new StubPlainTextExtractor();
    private static readonly ITranslationProvider StubProvider = new NoOpTranslationProvider();
    private static readonly TiptapTranslationHelper TranslationHelper = new();

    private class StubPlainTextExtractor : IPlainTextExtractor
    {
        public string? Extract(string? json) => json;
    }

    private static readonly ILogger<PageTranslationService> NullLogger = NullLogger<PageTranslationService>.Instance;

    private static PageTranslationService CreateService(AppDbContext ctx) =>
        new(ctx, StubProvider, StubExtractor, TranslationHelper, NullLogger);

    private static PageTranslationService CreateService(AppDbContext ctx, ITranslationProvider provider) =>
        new(ctx, provider, StubExtractor, TranslationHelper, NullLogger);

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
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static Page CreatePage(int id = 1, int spaceId = 1) => new()
    {
        Id = id,
        Title = "Pagina Teste",
        SpaceId = spaceId,
        SortOrder = 0,
        Content = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Texto\"}]}]}",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    private static async Task SeedBaseEntities(AppDbContext ctx)
    {
        ctx.Users.Add(CreateUser());
        ctx.Spaces.Add(CreateSpace());
        ctx.Pages.Add(CreatePage());
        await ctx.SaveChangesAsync();
    }

    // TEST 1: Generate translation for valid language
    [Fact]
    public async Task GenerateTranslationAsync_ValidLanguage_CreatesTranslation()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(GenerateTranslationAsync_ValidLanguage_CreatesTranslation));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        var result = await service.GenerateTranslationAsync(1, "en");

        result.Language.Should().Be("en");
        result.Status.Should().Be("automatica");
        result.PageId.Should().Be(1);
        result.Title.Should().NotBeEmpty();
    }

    // TEST 2: Reject original language
    [Fact]
    public async Task GenerateTranslationAsync_OriginalLanguage_ThrowsArgument()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(GenerateTranslationAsync_OriginalLanguage_ThrowsArgument));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        var act = () => service.GenerateTranslationAsync(1, "pt-BR");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*idioma original*");
    }

    // TEST 3: Reject unsupported language
    [Fact]
    public async Task GenerateTranslationAsync_UnsupportedLanguage_ThrowsArgument()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(GenerateTranslationAsync_UnsupportedLanguage_ThrowsArgument));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        var act = () => service.GenerateTranslationAsync(1, "fr");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*nao e suportado*");
    }

    // TEST 4: Duplicate non-outdated throws conflict
    [Fact]
    public async Task GenerateTranslationAsync_DuplicateNonOutdated_ThrowsConflict()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(GenerateTranslationAsync_DuplicateNonOutdated_ThrowsConflict));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.GenerateTranslationAsync(1, "en");

        var act = () => service.GenerateTranslationAsync(1, "en");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*traducao ativa*");
    }

    // TEST 5: Get existing translation
    [Fact]
    public async Task GetTranslationAsync_Exists_ReturnsTranslation()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(GetTranslationAsync_Exists_ReturnsTranslation));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.GenerateTranslationAsync(1, "en");

        var result = await service.GetTranslationAsync(1, "en");
        result.Language.Should().Be("en");
    }

    // TEST 6: Get non-existing throws
    [Fact]
    public async Task GetTranslationAsync_NotFound_Throws()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(GetTranslationAsync_NotFound_Throws));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        var act = () => service.GetTranslationAsync(1, "en");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // TEST 7: Update sets status revisada
    [Fact]
    public async Task UpdateTranslationAsync_ValidRequest_SetsRevisada()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(UpdateTranslationAsync_ValidRequest_SetsRevisada));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.GenerateTranslationAsync(1, "en");

        var result = await service.UpdateTranslationAsync(1, "en", new UpdateTranslationRequest
        {
            Title = "Updated Title",
            Description = "Updated Desc",
        });

        result.Status.Should().Be("revisada");
        result.Title.Should().Be("Updated Title");
    }

    // TEST 8: Delete soft-deletes
    [Fact]
    public async Task DeleteTranslationAsync_Exists_SoftDeletes()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(DeleteTranslationAsync_Exists_SoftDeletes));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.GenerateTranslationAsync(1, "en");

        await service.DeleteTranslationAsync(1, "en");

        var act = () => service.GetTranslationAsync(1, "en");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // TEST 9: MarkOutdated updates all active translations
    [Fact]
    public async Task MarkOutdatedAsync_UpdatesAllActiveTranslations()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(MarkOutdatedAsync_UpdatesAllActiveTranslations));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.GenerateTranslationAsync(1, "en");
        await service.GenerateTranslationAsync(1, "es");

        await service.MarkOutdatedAsync(1);

        var translations = await service.GetTranslationsAsync(1);
        translations.Should().HaveCount(2);
        translations.Should().OnlyContain(t => t.Status == "desatualizada");
    }

    // TEST 10: SoftDeleteByPage deletes all
    [Fact]
    public async Task SoftDeleteByPageAsync_DeletesAllTranslations()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(SoftDeleteByPageAsync_DeletesAllTranslations));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.GenerateTranslationAsync(1, "en");
        await service.GenerateTranslationAsync(1, "es");

        await service.SoftDeleteByPageAsync(1);

        var translations = await service.GetTranslationsAsync(1);
        translations.Should().BeEmpty();
    }

    // TEST 11: Regenerate outdated translation
    [Fact]
    public async Task GenerateTranslationAsync_OutdatedExists_Regenerates()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(GenerateTranslationAsync_OutdatedExists_Regenerates));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.GenerateTranslationAsync(1, "en");
        await service.MarkOutdatedAsync(1);

        var result = await service.GenerateTranslationAsync(1, "en");
        result.Status.Should().Be("automatica");
    }

    // TEST 12: AutoTranslate pt-BR page creates en and es translations
    [Fact]
    public async Task AutoTranslateAsync_NewPagePtBr_CreatesEnAndEsTranslations()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(AutoTranslateAsync_NewPagePtBr_CreatesEnAndEsTranslations));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.AutoTranslateAsync(1);

        var translations = await service.GetTranslationsAsync(1);
        translations.Should().HaveCount(2);
        translations.Select(t => t.Language).Should().BeEquivalentTo(["en", "es"]);
        translations.Should().OnlyContain(t => t.Status == "automatica");

        // Verify SourceHash is set
        var enEntity = await ctx.PageTranslations.FirstAsync(t => t.PageId == 1 && t.Language == "en");
        enEntity.SourceHash.Should().NotBeNullOrEmpty();
        var esEntity = await ctx.PageTranslations.FirstAsync(t => t.PageId == 1 && t.Language == "es");
        esEntity.SourceHash.Should().NotBeNullOrEmpty();
    }

    // TEST 13: AutoTranslate en page creates pt-BR and es translations
    [Fact]
    public async Task AutoTranslateAsync_NewPageEn_CreatesPtBrAndEsTranslations()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(AutoTranslateAsync_NewPageEn_CreatesPtBrAndEsTranslations));
        ctx.Users.Add(CreateUser());
        ctx.Spaces.Add(CreateSpace());
        var page = CreatePage();
        page.Language = "en";
        ctx.Pages.Add(page);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.AutoTranslateAsync(1);

        var translations = await service.GetTranslationsAsync(1);
        translations.Should().HaveCount(2);
        translations.Select(t => t.Language).Should().BeEquivalentTo(["pt-BR", "es"]);
        translations.Should().OnlyContain(t => t.Status == "automatica");
    }

    // TEST 14: AutoTranslate with unchanged hash skips translation
    [Fact]
    public async Task AutoTranslateAsync_HashUnchanged_SkipsTranslation()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(AutoTranslateAsync_HashUnchanged_SkipsTranslation));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.AutoTranslateAsync(1);

        var enBefore = await ctx.PageTranslations.AsNoTracking().FirstAsync(t => t.PageId == 1 && t.Language == "en");
        var updatedAtBefore = enBefore.UpdatedAt;

        // Wait a tiny bit so UpdatedAt would differ if changed
        await Task.Delay(50);

        // Call again without changing page content
        await service.AutoTranslateAsync(1);

        var enAfter = await ctx.PageTranslations.AsNoTracking().FirstAsync(t => t.PageId == 1 && t.Language == "en");
        enAfter.UpdatedAt.Should().Be(updatedAtBefore);
    }

    // TEST 15: AutoTranslate with changed hash re-translates automatic translations
    [Fact]
    public async Task AutoTranslateAsync_HashChanged_AutomaticStatus_RetranslatesAndUpdatesHash()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(AutoTranslateAsync_HashChanged_AutomaticStatus_RetranslatesAndUpdatesHash));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.AutoTranslateAsync(1);

        var hashBefore = (await ctx.PageTranslations.AsNoTracking().FirstAsync(t => t.PageId == 1 && t.Language == "en")).SourceHash;

        // Change page content
        var page = await ctx.Pages.FirstAsync(p => p.Id == 1);
        page.Content = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Novo conteudo\"}]}]}";
        await ctx.SaveChangesAsync();

        await service.AutoTranslateAsync(1);

        var enAfter = await ctx.PageTranslations.AsNoTracking().FirstAsync(t => t.PageId == 1 && t.Language == "en");
        enAfter.SourceHash.Should().NotBe(hashBefore);
        enAfter.Status.Should().Be("automatica");
    }

    // TEST 16: AutoTranslate with changed hash marks revisada as desatualizada
    [Fact]
    public async Task AutoTranslateAsync_HashChanged_RevisadaStatus_MarksOutdated()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(AutoTranslateAsync_HashChanged_RevisadaStatus_MarksOutdated));
        await SeedBaseEntities(ctx);

        var service = CreateService(ctx);
        await service.AutoTranslateAsync(1);

        // Manually set EN translation to revisada
        var enTranslation = await ctx.PageTranslations.FirstAsync(t => t.PageId == 1 && t.Language == "en");
        enTranslation.Status = "revisada";
        await ctx.SaveChangesAsync();

        // Change page content
        var page = await ctx.Pages.FirstAsync(p => p.Id == 1);
        page.Content = "{\"type\":\"doc\",\"content\":[{\"type\":\"paragraph\",\"content\":[{\"type\":\"text\",\"text\":\"Conteudo alterado\"}]}]}";
        await ctx.SaveChangesAsync();

        await service.AutoTranslateAsync(1);

        var enAfter = await ctx.PageTranslations.AsNoTracking().FirstAsync(t => t.PageId == 1 && t.Language == "en");
        enAfter.Status.Should().Be("desatualizada");

        // ES should still be re-translated (was automatica)
        var esAfter = await ctx.PageTranslations.AsNoTracking().FirstAsync(t => t.PageId == 1 && t.Language == "es");
        esAfter.Status.Should().Be("automatica");
    }

    // TEST 17: AutoTranslate partial failure translates successful languages only
    [Fact]
    public async Task AutoTranslateAsync_PartialFailure_TranslatesSuccessfulLanguages()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(AutoTranslateAsync_PartialFailure_TranslatesSuccessfulLanguages));
        await SeedBaseEntities(ctx);

        var failProvider = new SelectiveFailTranslationProvider("es");
        var service = CreateService(ctx, failProvider);
        await service.AutoTranslateAsync(1);

        var translations = await service.GetTranslationsAsync(1);
        translations.Should().HaveCount(1);
        translations[0].Language.Should().Be("en");
    }

    // TEST 18: AutoTranslate page without content translates title and description
    [Fact]
    public async Task AutoTranslateAsync_PageWithoutContent_TranslatesTitleAndDescription()
    {
        await using var ctx = TestDbContextFactory.Create(nameof(AutoTranslateAsync_PageWithoutContent_TranslatesTitleAndDescription));
        ctx.Users.Add(CreateUser());
        ctx.Spaces.Add(CreateSpace());
        ctx.Pages.Add(new Page
        {
            Id = 1,
            Title = "Titulo da Pagina",
            Description = "Descricao da pagina",
            Content = null,
            SpaceId = 1,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        await service.AutoTranslateAsync(1);

        var translations = await service.GetTranslationsAsync(1);
        translations.Should().HaveCount(2);

        var enEntity = await ctx.PageTranslations.AsNoTracking().FirstAsync(t => t.PageId == 1 && t.Language == "en");
        enEntity.Title.Should().Contain("[AUTO-en]");
        enEntity.Content.Should().BeNull();
        enEntity.Description.Should().Contain("[AUTO-en]");
    }

    private class SelectiveFailTranslationProvider(string failLanguage) : ITranslationProvider
    {
        public Task<TranslationResult> TranslateTextAsync(string text, string fromLang, string toLang)
        {
            if (toLang == failLanguage)
                return Task.FromResult(new TranslationResult("", false, "Simulated failure"));
            return Task.FromResult(new TranslationResult($"[AUTO-{toLang}] {text}", true));
        }
    }
}
