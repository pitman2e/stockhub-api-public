using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Interfaces;
using System.Linq;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Services;

/// <summary>
/// [AI-Generated]
/// </summary>
[TestSubject(typeof(TagsService))]
public class TagsServiceTests
{
    private readonly IUserClaims _userClaims = UserClaimMock.Get();

    private StockHubContext CreateInMemoryContext()
    {
        var db = DbContextMock.Get();
        DatabaseSetup.AddStock(db, "AAPL.US", "USD");
        DatabaseSetup.AddStock(db, "MSFT.US", "USD");
        DatabaseSetup.AddStock(db, "VT.US", "USD");
        return db;
    }

    [Fact]
    public async Task GetTagsCsvAsync_ReturnsCsvFormattedString_WhenTagsExist()
    {
        await using var context = CreateInMemoryContext();
        context.StockTags.AddRange(
            new StockTag
            {
                Uid = _userClaims.GetUid(),
                StockId = "AAPL.US",
                TagCategory = "SECTOR",
                Tag = "Tech",
                Percentage = 50
            },
            new StockTag
            {
                Uid = _userClaims.GetUid(),
                StockId = "MSFT.US",
                TagCategory = "SECTOR",
                Tag = "Tech",
                Percentage = 50
            }
        );
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var service = new TagsService(context, _userClaims);

        var result = await service.GetTagsCsvAsync("SECTOR");

        Assert.NotNull(result);
        Assert.Contains("AAPL.US", result);
        Assert.Contains("MSFT.US", result);
        Assert.Contains("Tech", result);
    }

    [Fact]
    public async Task SaveTagsCsvAsync_ParsesCsvAndSavesStockTags()
    {
        await using var context = CreateInMemoryContext();
        var service = new TagsService(context, _userClaims);

        const string category = "COUNTRY";
        const string csvContent = """
                                  VT.US, United States, 61.60, 
                                  VT.US, Japan, 5.80, 
                                  VT.US, United Kingdom, 3.30, 
                                  VT.US, Canada, 3.10, 
                                  VT.US, China, 3.00, 
                                  VT.US, Taiwan, 3.00, 
                                  VT.US, Korea, 2.30, 
                                  VT.US, France, 2.00, 
                                  VT.US, Switzerland, 1.90,
                                  """;

        await service.SaveTagsCsvAsync(category, csvContent);

        var savedTags = await context.StockTags
            .Where(t => t.Uid == _userClaims.GetUid() && t.TagCategory == category)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(9, savedTags.Count);
        Assert.Equal("VT.US", savedTags[0].StockId);
        Assert.Equal("United States", savedTags[0].Tag);
        Assert.Equal(61.6m, savedTags[0].Percentage);
    }
}