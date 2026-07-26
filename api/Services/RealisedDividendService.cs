using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Tools;

namespace StockHub.Services;

public class RealisedDividendService(
    Stock2ExchangeService stock2ExchangeService,
    //IUserClaims userClaims,
    StockHubContext context)
{
    public async Task<IEnumerable<RealisedDividend>> GetRealisedDividendsAsync(
        UPSFilter upsFilter,
        List<Expression<Func<StockTransaction, bool>>> filters,
        DateOnly? cutOffDate = null)
    {
        var rtv = new List<RealisedDividend>();
        
        IQueryable<StockTransaction> query = context.StockTransactions_ByUPS(upsFilter);
        foreach (var filter in filters)
        {
            query = query.Where(filter);
        }
        var transRange = await query
            .Where(st => st.TranType == StockTransaction.TRANTYPE_BUY || 
                         st.TranType == StockTransaction.TRANTYPE_SELL || 
                         st.TranType == StockTransaction.TRANTYPE_REINV)
            .Where(st => cutOffDate == null || st.TxDate <= cutOffDate.Value)
            .GroupBy(st => new { st.Uid, st.PortfolioId, st.StockId })
            .Select(g => new
            {
                Uid = g.Key.Uid,
                PortfolioId = g.Key.PortfolioId,
                StockId = g.Key.StockId,
                TxFrom = g.Min(t => t.TxDate)
            })
            .ToListAsync();
        
        foreach (var tranRange in transRange)
        {
            var dividendsQuery = 
                from sd in context.StockDividends.Include(d => d.FkStockRealisedScrips)
                join st in context.Stocks on sd.StockId equals st.StockId
                join sp in context.StockPrices 
                    on new { sd.StockId, JoinDate = sd.ExDate } equals new { sp.StockId, JoinDate = sp.MarketDate }
                    into sdst
                from sdstLeftJoin in sdst.DefaultIfEmpty()
                join pos in context.StockPositions
                    on new { st.StockId, tranRange.Uid, tranRange.PortfolioId } 
                    equals new { pos.StockId, pos.Uid, pos.PortfolioId }
                    into sdpos
                where sd.StockId == tranRange.StockId
                where cutOffDate == null || sd.PayableDate <= cutOffDate.Value
                where sd.ExDate >= tranRange.TxFrom
                
                // Using 'let' bindings to keep the query clean and readable
                let transUnit = sdpos
                    .Where(p => p.ObserveDate <= sd.ExDate.AddDays(-1))
                    .OrderByDescending(p => p.ObserveDate)
                    .Select(p => (decimal?)p.Quantity)
                    .FirstOrDefault() ?? 0
                    
                // Apply transUnit > 0 condition directly to the database query
                where transUnit > 0 

                let scripDivToPay = sd.FkStockRealisedScrips
                    .Where(r => r.PortfolioId == tranRange.PortfolioId) 
                    .Select(r => r.ScripReceived)
                    .FirstOrDefault()

                let reinvestPrice = sd.FkStockRealisedScrips
                    .Where(r => r.PortfolioId == tranRange.PortfolioId) 
                    .Select(r => r.ReinvestPrice)
                    .FirstOrDefault()

                orderby sd.PayableDate descending

                // Map straight to the model natively supported by Entity Framework Core
                select new RealisedDividend
                {
                    Uid = tranRange.Uid,
                    PortfolioId = tranRange.PortfolioId,
                    DividendId = sd.DividendId,
                    StockId = sd.StockId,
                    StockName = st.StockName,
                    Cnt = transUnit,
                    ExDate = sd.ExDate,
                    PayDate = sd.PayableDate,
                    DividendEvent = sd.DividendEvent,
                    DividendType = sd.DividendType,
                    AmountAdjPercentage = sd.AmountAdjPercentage,
                    PayPerUnit = sd.Amount ?? 0, 
                    DistributionType = sd.DistributionType,
                    ScripReceived = scripDivToPay,
                    Currency = sd.Currency,
                    ScripPrice = sd.ScripPrice,
                    ReinvestPrice = reinvestPrice,
                    StockClosePrice = sdstLeftJoin == null ? 0 : sdstLeftJoin.ClosePrice,
                    DividendYield = sdstLeftJoin == null ? null : (sd.Amount / sdstLeftJoin.ClosePrice) * 100
                };

            // Switch to client-side evaluation to run C# exclusive methods and math
            rtv.AddRange(dividendsQuery.AsEnumerable().Select(divItem =>
            {
                divItem.isMissingScripPrice = (divItem.ScripPrice ?? 0) == 0 && 
                                              divItem.ScripReceived > 0 && 
                                              (divItem.ReinvestPrice ?? 0) <= 0;

                divItem.PretaxTotalAmt = divItem.isMissingScripPrice 
                    ? 0 
                    : (divItem.PayPerUnit * divItem.Cnt) - ((divItem.ScripPrice ?? 0) * divItem.ScripReceived);

                if (divItem.PretaxTotalAmt < 0)
                {
                    divItem.PretaxTotalAmt = 0; //#Workaround Scrip price is not precision enough causing this val becomes less than 0
                }

                if (stock2ExchangeService.ParseExact(divItem.StockId).Exchange.MarketId == US.MARKET_ID)
                {
                    divItem.TotalAmt = 0.7m * divItem.PretaxTotalAmt;
                }
                else
                {
                    divItem.TotalAmt = divItem.PretaxTotalAmt;
                }

                return divItem;
            }));
        }

        return rtv.OrderByDescending(t => t.PayDate);
    }
}