using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Infrastructure.Utilities;
using Xunit;

namespace PreflightApi.Tests.Utilities;

public class PaginationExtensionsTests : IDisposable
{
    private readonly TestPaginationDbContext _dbContext;

    public PaginationExtensionsTests()
    {
        var options = new DbContextOptionsBuilder<TestPaginationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestPaginationDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    // --- String key tests ---

    [Fact]
    public async Task StringKey_NoCursor_ReturnsFirstPage_NoPrevious()
    {
        SeedStringItems("A", "B", "C", "D", "E");

        var result = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, null, 3);

        result.Data.Should().BeEquivalentTo(["A", "B", "C"], o => o.WithStrictOrdering());
        result.Pagination.HasMore.Should().BeTrue();
        result.Pagination.NextCursor.Should().NotBeNull();
        result.Pagination.HasPrevious.Should().BeFalse();
        result.Pagination.PreviousCursor.Should().BeNull();
    }

    [Fact]
    public async Task StringKey_ForwardCursor_ReturnsMiddlePage_BothCursors()
    {
        SeedStringItems("A", "B", "C", "D", "E");

        // Get first page
        var page1 = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, null, 2);

        // Navigate forward
        var page2 = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, page1.Pagination.NextCursor, 2);

        page2.Data.Should().BeEquivalentTo(["C", "D"], o => o.WithStrictOrdering());
        page2.Pagination.HasMore.Should().BeTrue();
        page2.Pagination.NextCursor.Should().NotBeNull();
        page2.Pagination.HasPrevious.Should().BeTrue();
        page2.Pagination.PreviousCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task StringKey_ForwardLastPage_NoNextCursor_HasPrevious()
    {
        SeedStringItems("A", "B", "C");

        var page1 = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, null, 2);

        var page2 = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, page1.Pagination.NextCursor, 2);

        page2.Data.Should().BeEquivalentTo(["C"], o => o.WithStrictOrdering());
        page2.Pagination.HasMore.Should().BeFalse();
        page2.Pagination.NextCursor.Should().BeNull();
        page2.Pagination.HasPrevious.Should().BeTrue();
        page2.Pagination.PreviousCursor.Should().NotBeNull();
    }

    [Fact]
    public async Task StringKey_BackwardFromPage2_ReturnsPage1()
    {
        SeedStringItems("A", "B", "C", "D", "E");

        // Forward to page 2
        var page1 = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, null, 2);
        var page2 = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, page1.Pagination.NextCursor, 2);

        // Navigate backward using previousCursor
        var backToPage1 = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, page2.Pagination.PreviousCursor, 2);

        backToPage1.Data.Should().BeEquivalentTo(["A", "B"], o => o.WithStrictOrdering());
        backToPage1.Pagination.HasPrevious.Should().BeFalse();
        backToPage1.Pagination.PreviousCursor.Should().BeNull();
        backToPage1.Pagination.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task StringKey_EmptyDataset_BothCursorsNull()
    {
        var result = await _dbContext.StringItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Key, e => e.Key, null, 10);

        result.Data.Should().BeEmpty();
        result.Pagination.NextCursor.Should().BeNull();
        result.Pagination.HasMore.Should().BeFalse();
        result.Pagination.PreviousCursor.Should().BeNull();
        result.Pagination.HasPrevious.Should().BeFalse();
    }

    // --- Int key tests ---

    [Fact]
    public async Task IntKey_NoCursor_ReturnsFirstPage()
    {
        SeedIntItems(1, 2, 3, 4, 5);

        var result = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, null, 3);

        result.Data.Should().BeEquivalentTo([1, 2, 3], o => o.WithStrictOrdering());
        result.Pagination.HasMore.Should().BeTrue();
        result.Pagination.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task IntKey_RoundTrip_ForwardThenBackward()
    {
        SeedIntItems(1, 2, 3, 4, 5);

        // Page 1
        var page1 = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, null, 2);
        page1.Data.Should().BeEquivalentTo([1, 2], o => o.WithStrictOrdering());

        // Page 2
        var page2 = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, page1.Pagination.NextCursor, 2);
        page2.Data.Should().BeEquivalentTo([3, 4], o => o.WithStrictOrdering());

        // Back to page 1
        var back = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, page2.Pagination.PreviousCursor, 2);
        back.Data.Should().BeEquivalentTo([1, 2], o => o.WithStrictOrdering());
        back.Pagination.HasPrevious.Should().BeFalse();
        back.Pagination.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task IntKey_BackwardPage_ItemsInAscendingOrder()
    {
        SeedIntItems(1, 2, 3, 4, 5, 6, 7);

        // Forward to page 3
        var p1 = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, null, 2);
        var p2 = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, p1.Pagination.NextCursor, 2);
        var p3 = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, p2.Pagination.NextCursor, 2);

        p3.Data.Should().BeEquivalentTo([5, 6], o => o.WithStrictOrdering());

        // Backward should return items in ascending order
        var backToP2 = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, p3.Pagination.PreviousCursor, 2);

        backToP2.Data.Should().BeEquivalentTo([3, 4], o => o.WithStrictOrdering());
        backToP2.Pagination.HasPrevious.Should().BeTrue();
        backToP2.Pagination.HasMore.Should().BeTrue();
    }

    // --- Guid key tests ---

    [Fact]
    public async Task GuidKey_ForwardAndBackward_RoundTrips()
    {
        var guids = Enumerable.Range(0, 5)
            .Select(_ => Guid.NewGuid())
            .OrderBy(g => g)
            .ToList();

        foreach (var g in guids)
            _dbContext.GuidItems.Add(new GuidKeyEntity { Id = g });
        _dbContext.SaveChanges();

        // Page 1
        var page1 = await _dbContext.GuidItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, null, 2);
        page1.Data.Should().HaveCount(2);
        page1.Pagination.HasMore.Should().BeTrue();
        page1.Pagination.HasPrevious.Should().BeFalse();

        // Page 2
        var page2 = await _dbContext.GuidItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, page1.Pagination.NextCursor, 2);
        page2.Data.Should().HaveCount(2);
        page2.Pagination.HasPrevious.Should().BeTrue();

        // Back to page 1
        var back = await _dbContext.GuidItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, page2.Pagination.PreviousCursor, 2);
        back.Data.Should().BeEquivalentTo(page1.Data, o => o.WithStrictOrdering());
        back.Pagination.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task GuidKey_EmptyDataset_BothCursorsNull()
    {
        var result = await _dbContext.GuidItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, null, 10);

        result.Data.Should().BeEmpty();
        result.Pagination.NextCursor.Should().BeNull();
        result.Pagination.HasMore.Should().BeFalse();
        result.Pagination.PreviousCursor.Should().BeNull();
        result.Pagination.HasPrevious.Should().BeFalse();
    }

    // --- Legacy cursor backward compat ---

    [Fact]
    public async Task IntKey_LegacyCursor_TreatedAsForward()
    {
        SeedIntItems(1, 2, 3, 4, 5);

        // Old-style cursor (no direction prefix)
        var legacyCursor = CursorHelper.Encode(2);

        var result = await _dbContext.IntItems.AsNoTracking()
            .ToPaginatedAsync(e => e.Id, e => e.Id, legacyCursor, 2);

        result.Data.Should().BeEquivalentTo([3, 4], o => o.WithStrictOrdering());
        result.Pagination.HasMore.Should().BeTrue();
        result.Pagination.HasPrevious.Should().BeTrue();
    }

    // --- Helpers ---

    private void SeedStringItems(params string[] keys)
    {
        foreach (var key in keys)
            _dbContext.StringItems.Add(new StringKeyEntity { Key = key });
        _dbContext.SaveChanges();
    }

    private void SeedIntItems(params int[] ids)
    {
        foreach (var id in ids)
            _dbContext.IntItems.Add(new IntKeyEntity { Id = id });
        _dbContext.SaveChanges();
    }
}

// --- Test-only entities and DbContext ---

public class StringKeyEntity
{
    [Key]
    public string Key { get; set; } = null!;
}

public class IntKeyEntity
{
    [Key]
    public int Id { get; set; }
}

public class GuidKeyEntity
{
    [Key]
    public Guid Id { get; set; }
}

public class TestPaginationDbContext : DbContext
{
    public TestPaginationDbContext(DbContextOptions<TestPaginationDbContext> options) : base(options) { }

    public DbSet<StringKeyEntity> StringItems => Set<StringKeyEntity>();
    public DbSet<IntKeyEntity> IntItems => Set<IntKeyEntity>();
    public DbSet<GuidKeyEntity> GuidItems => Set<GuidKeyEntity>();
}
