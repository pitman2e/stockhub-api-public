using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Interfaces;
using StockHub.Extensions;

namespace StockHub.Services;

public class TagsService(
    StockHubContext context,
    IUserClaims userClaims)
{
    public async Task<string> GetTagsCsvAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new SHArgumentException("Tag must not be empty");
        }

        if (category == "CLASS")
        {
            throw new SHArgumentException("Tag Category 'Class' is not editable");
        }

        var result = await context.StockTags
                    .Where(t => t.TagCategory == category)
                    .OrderBy(t => t.StockId)
                    .ThenByDescending(t => t.Percentage)
                    .Select(t => string.Join(", ", new[] { t.StockId, t.Tag, t.Percentage.ToString(CultureInfo.InvariantCulture), t.Color }))
                    .ToListAsync();

        return string.Join(Environment.NewLine, result);
    }

    public async Task<IEnumerable<StockTag>> SaveTagsCsvAsync(string category, string csv)
    {
        var nowDate = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new SHArgumentException("Tag must not be empty");
        }

        if (category == "CLASS")
        {
            throw new SHArgumentException("Tag Category 'Class' is not editable");
        }

        var tags = new List<StockTag>();
        try
        {
            foreach (var item in csv.Split("\n"))
            {
                if (item.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var rowitems = item.Split(",");
                var tag = new StockTag
                {
                    Uid = userClaims.GetUid(),
                    TagCategory = category.Trim().ToUpper(),
                    StockId = rowitems[0].Trim(),
                    Tag = rowitems[1].Trim(),
                    Percentage = Convert.ToDecimal(rowitems[2].Trim()),
                    Color = rowitems[3].Trim(),
                    UpdatedAt = nowDate
                };
                tags.Add(tag);
            }
        }
        catch (Exception)
        {
            throw new SHArgumentException("Error parsing the csv string");
        }

        var distinctStockId = tags.Select(t => t.StockId).Distinct();

        var dbStockIdCnt = context.Stocks.Count(t => distinctStockId.Contains(t.StockId));

        if (distinctStockId.Count() != dbStockIdCnt)
        {
            throw new SHArgumentException("Invalid Stock Id in csv string");
        }

        await using var dbTrans = await context.Database.BeginTransactionAsync();
        context.RemoveRange(context.StockTags.Where(t => t.TagCategory == category));
        context.AddRange(tags);
        await context.SaveChangesAsync();
        await dbTrans.CommitAsync();

        return tags;
    }
}