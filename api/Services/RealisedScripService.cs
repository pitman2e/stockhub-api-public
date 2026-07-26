using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Interfaces;

namespace StockHub.Services;

public class RealisedScripService(
    StockHubContext context,
    IUserClaims userClaims)
{
    public async Task CalculateRealisedScripPerAmountAsync(string portfolioId, string stockId)
    {
        var uid = userClaims.GetUid();

        var stockIds = await context.StockTransactions
            .ByUid(uid, isNullable: true)
            .ByPortfolioId_Real(portfolioId, isNullable: false)
            .ByStockId(stockId, isNullable: true)
            .Select(t => new { StockId = t.StockId, Uid = t.Uid })
            .Distinct()
            .ToListAsync();

        foreach (var runningStockId in stockIds)
        {
            var portfolioIds = await context.StockTransactions
                .ByUid(runningStockId.Uid, isNullable: false)
                .ByPortfolioId_Real(portfolioId, isNullable: true)
                .ByStockId(runningStockId.StockId, isNullable: false)
                .Select(t => new { PortfolioId = t.PortfolioId, Uid = t.Uid })
                .Distinct()
                .ToListAsync();

            foreach (var runningPortfolioId in portfolioIds)
            {
                var bonuses = await context.StockDividends
                    .ByStockId(runningStockId.StockId, isNullable: false)
                    .Where(d => d.DividendType == StockDividend.DIV_TYPE_BONUS)
                    .OrderBy(d => d.ExDate)
                    .ToListAsync();

                var bonusAccuCount = 0;

                foreach (var bonus in bonuses)
                {
                    var entitledCnt = await context.StockTransactions
                                      .ByUid(runningPortfolioId.Uid, isNullable: false)
                                      .ByPortfolioId_Real(runningPortfolioId.PortfolioId, isNullable: false)
                                      .ByStockId(bonus.StockId, isNullable: false)
                                      .Where(t => t.TxDate < bonus.ExDate)
                                      .Select(t => t.TxCount)
                                      .SumAsync() + bonusAccuCount;

                    var bonusScrip = (int)(entitledCnt / bonus.ScripPerCount);
                    var realisedScrip = new StockRealisedScrip
                    {
                        Uid = runningPortfolioId.Uid,
                        PortfolioId = runningPortfolioId.PortfolioId,
                        DistributionType = bonus.DistributionType,
                        DividendType = bonus.DividendType,
                        PayableDate = bonus.PayableDate,
                        StockId = bonus.StockId
                    };

                    var dbRealisedScrip = await context.StockRealisedScrips.FirstOrDefaultAsync(
                        d => 
                        d.Uid == realisedScrip.Uid &&
                        d.PortfolioId == realisedScrip.PortfolioId &&
                        d.StockId == realisedScrip.StockId &&
                        d.PayableDate == realisedScrip.PayableDate &&
                        d.DividendType == realisedScrip.DividendType &&
                        d.DistributionType == realisedScrip.DistributionType
                        );

                    if (dbRealisedScrip == null)
                    {
                        realisedScrip.ScripReceived = bonusScrip;
                        context.StockRealisedScrips.Add(realisedScrip);
                    }
                    else
                    {
                        dbRealisedScrip.ScripReceived = bonusScrip;
                        context.StockRealisedScrips.Update(dbRealisedScrip);
                    }

                    bonusAccuCount += bonusScrip;
                }

                await context.SaveChangesAsync();
            }
        }
    }

    public async Task UpsertRealisedScripAsync(StockDividend dividend, string portfolioId, decimal decScripReceived, decimal? decReinvestPrice) {
        var realisedScrip = new StockRealisedScrip
        {
            Uid = userClaims.GetUid(),
            DistributionType = dividend.DistributionType,
            DividendType = dividend.DividendType,
            PayableDate = dividend.PayableDate,
            StockId = dividend.StockId,
            PortfolioId = portfolioId
        };

        var dbRealisedScrip = await context.StockRealisedScrips.FirstOrDefaultAsync(
            d => 
            d.Uid == realisedScrip.Uid &&
            d.StockId == realisedScrip.StockId &&
            d.PayableDate == realisedScrip.PayableDate &&
            d.DividendType == realisedScrip.DividendType &&
            d.DistributionType == realisedScrip.DistributionType &&
            d.PortfolioId == realisedScrip.PortfolioId
            );

        if (dbRealisedScrip == null)
        {
            realisedScrip.ScripReceived = decScripReceived;
            realisedScrip.ReinvestPrice = decReinvestPrice;
            context.StockRealisedScrips.Add(realisedScrip);
        }
        else
        {
            dbRealisedScrip.ScripReceived = decScripReceived;
            dbRealisedScrip.ReinvestPrice = decReinvestPrice;
            context.StockRealisedScrips.Update(dbRealisedScrip);
        }

        await context.SaveChangesAsync();
    }
}