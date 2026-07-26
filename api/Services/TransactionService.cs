using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockHub.Database;

namespace StockHub.Services;

public class TransactionService(
    StockHubContext context,
    //IUserClaims userClaims,
    ILogger<TransactionService> logger)
{
    public async Task UpdateStockTxMinMaxAsync(string stockId)
    {
        var stockMetadata = await context.StockMetadata.FindAsync(stockId);

        if (stockMetadata == null)
        {
            logger.LogWarning($"Stock Metadata for {stockId} not found, creating...");
            stockMetadata = new StockMetadata
            {
                StockId = stockId
            };
            context.StockMetadata.Add(stockMetadata);
        }

        var query = context.StockTransactions
            .Where(t => t.StockId == stockId);
        
        var minDate = await query.MinAsync(t => t.TxDate);
        var maxDate = await query.MaxAsync(t => t.TxDate);

        stockMetadata.TxMinDate = minDate;
        stockMetadata.TxMaxDate = maxDate;
        
        await context.SaveChangesAsync();
    }
}