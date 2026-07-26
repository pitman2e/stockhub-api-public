using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Controllers.Dividend;
using StockHub.Database;
using StockHub.Errors;

namespace StockHub.Repositories;

public class DividendRepo(
    StockHubContext context)
{
    public async Task<List<StockDividend>> GetDividendsAsync(List<string> stockIds2Search)
    {
        return await (
            from d in context.StockDividends
            where stockIds2Search.Contains(d.StockId)
            orderby d.PayableDate descending
            select d
        ).ToListAsync();
    }

    public async Task UpdateScripPriceAsync(DividendPutDto dto)
    {
        if (dto.ScripPrice != null && dto.ScripPrice < 0)
        {
            throw new SHArgumentException("Scrip price must not be a negative number");
        }

        var dividend = await context.StockDividends.FirstOrDefaultAsync(d => d.DividendId == dto.DividendId);

        if (dividend == null)
        {
            throw new SHArgumentException("Corresponding stock dividend entry not found");
        }

        if (!dividend.DistributionType.Contains(StockDividend.DIST_TYPE_SCRIP))
        {
            throw new SHArgumentException("Corresponding stock dividend distribution type is not 'Scrip'");
        }

        dividend.ScripPrice = dto.ScripPrice;

        context.Update(dividend);
        await context.SaveChangesAsync();
    }
}