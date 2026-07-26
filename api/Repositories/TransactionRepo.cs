using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Tools;

namespace StockHub.Repositories;

public class TransactionRepo(StockHubContext context)
{
    public List<StockTransaction> Get(UPSFilter upsFilter, DateOnly? dateFmDate = null, DateOnly? dateToDate = null)
    {
        var stockTrans = (from t in context.StockTransactions_ByUPS(upsFilter)
                    .Include(t => t.FkStockPortfolio)
                    .Include(t => t.FkStock)
                where dateToDate == null || t.TxDate <= dateToDate
                where dateFmDate == null || t.TxDate >= dateFmDate
                select t)
            .ToList();

        return stockTrans;
    }
    
    public async Task<List<string>> GetTransactedStockIdsAsync(UPSFilter upsFilter)
    {
        return await context.StockTransactions_ByUPS(upsFilter)
            .Select(t => t.StockId)
            .Distinct()
            .ToListAsync();
    }
}