using DocMigrate.Application.DTOs.Reference;
using DocMigrate.Domain.Entities;
using DocMigrate.Infrastructure.Services;
using DocMigrate.Tests.Helpers;
using FluentAssertions;

namespace DocMigrate.Tests;

public class ReferenceServiceTests
{
    private static Space CreateSpace(int id, string title = "Space") => new()
    {
        Id = id,
        Title = title,
    };

    private static Page CreatePage(int id, int spaceId = 1, string title = "Page") => new()
    {
        Id = id,
        Title = title,
        SpaceId = spaceId,
        SortOrder = 0,
    };

    #region CheckAsync

    [Fact]
    public async Task CheckAsync_EmptyLists_ReturnsEmptyLists()
    {
        await using var context = TestDbContextFactory.Create(nameof(CheckAsync_EmptyLists_ReturnsEmptyLists));
        var service = new ReferenceService(context);

        var result = await service.CheckAsync(new CheckReferencesRequest());

        result.ExistingPageIds.Should().BeEmpty();
        result.ExistingSpaceIds.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAsync_ExistingIds_ReturnsExistingOnly()
    {
        await using var context = TestDbContextFactory.Create(nameof(CheckAsync_ExistingIds_ReturnsExistingOnly));
        context.Spaces.Add(CreateSpace(1));
        context.Pages.AddRange(CreatePage(10, 1), CreatePage(20, 1));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(CheckAsync_ExistingIds_ReturnsExistingOnly));
        var service = new ReferenceService(readContext);

        var result = await service.CheckAsync(new CheckReferencesRequest
        {
            PageIds = [10, 20, 999],
            SpaceIds = [1, 888],
        });

        result.ExistingPageIds.Should().BeEquivalentTo([10, 20]);
        result.ExistingSpaceIds.Should().BeEquivalentTo([1]);
    }

    [Fact]
    public async Task CheckAsync_SoftDeletedEntities_ExcludesDeleted()
    {
        await using var context = TestDbContextFactory.Create(nameof(CheckAsync_SoftDeletedEntities_ExcludesDeleted));
        var space = CreateSpace(1);
        var activePage = CreatePage(10, 1);
        var deletedPage = CreatePage(20, 1);
        deletedPage.DeletedAt = DateTime.UtcNow;
        var deletedSpace = CreateSpace(2);
        deletedSpace.DeletedAt = DateTime.UtcNow;
        context.Spaces.AddRange(space, deletedSpace);
        context.Pages.AddRange(activePage, deletedPage);
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(CheckAsync_SoftDeletedEntities_ExcludesDeleted));
        var service = new ReferenceService(readContext);

        var result = await service.CheckAsync(new CheckReferencesRequest
        {
            PageIds = [10, 20],
            SpaceIds = [1, 2],
        });

        result.ExistingPageIds.Should().BeEquivalentTo([10]);
        result.ExistingSpaceIds.Should().BeEquivalentTo([1]);
    }

    [Fact]
    public async Task CheckAsync_OnlyPageIds_ReturnsOnlyPages()
    {
        await using var context = TestDbContextFactory.Create(nameof(CheckAsync_OnlyPageIds_ReturnsOnlyPages));
        context.Spaces.Add(CreateSpace(1));
        context.Pages.Add(CreatePage(10, 1));
        await context.SaveChangesAsync();

        await using var readContext = TestDbContextFactory.Create(nameof(CheckAsync_OnlyPageIds_ReturnsOnlyPages));
        var service = new ReferenceService(readContext);

        var result = await service.CheckAsync(new CheckReferencesRequest
        {
            PageIds = [10],
        });

        result.ExistingPageIds.Should().BeEquivalentTo([10]);
        result.ExistingSpaceIds.Should().BeEmpty();
    }

    #endregion

    #region Validator

    [Fact]
    public void Validator_PageIdsExceedsLimit_ReturnsError()
    {
        var validator = new Application.Validators.CheckReferencesRequestValidator();
        var request = new CheckReferencesRequest
        {
            PageIds = Enumerable.Range(1, 101).ToList(),
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("100");
    }

    [Fact]
    public void Validator_SpaceIdsExceedsLimit_ReturnsError()
    {
        var validator = new Application.Validators.CheckReferencesRequestValidator();
        var request = new CheckReferencesRequest
        {
            SpaceIds = Enumerable.Range(1, 101).ToList(),
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("100");
    }

    [Fact]
    public void Validator_WithinLimits_IsValid()
    {
        var validator = new Application.Validators.CheckReferencesRequestValidator();
        var request = new CheckReferencesRequest
        {
            PageIds = Enumerable.Range(1, 100).ToList(),
            SpaceIds = Enumerable.Range(1, 50).ToList(),
        };

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    #endregion
}
