using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Tools;

namespace StockHub.Repositories;

public class PositionRepo(
    StockHubContext context)
{
    public async Task<List<string>> GetOpenPositionStockIdsAsync(UPSFilter upsFilter)
    {
        return await context.StockPositions_ByUPS(upsFilter)
            .Where(p => p.IsLatest)
            .Where(p => p.Quantity > 0)
            .Select(t => t.StockId)
            .Distinct()
            .ToListAsync();
    }
}