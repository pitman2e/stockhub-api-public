using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Tools;

namespace StockHub.Services.Position;

public partial class PositionValueService(
    StockHubContext context)
{
    public async Task<PositionChartData> GetPositionChartDataAsync(
        UPSFilter upsFilter,
        DateOnly dateFmDate,
        DateOnly dateToDate,
        int dayRes = 1)
    {
        var stockPositionValues = await GetStockPositionValuesAsync(
            upsFilter: upsFilter,
            dateFmDate: dateFmDate,
            dateToDate: dateToDate,
            isSkipNonmarketDate: false,
            positionStatus: PositionStatus.Any);

        return new PositionChartData(stockPositionValues, dayRes);
    }

    public async Task<List<StockPositionValue>> GetLatestPositionsValueAsync(
        UPSFilter upsFilter,
        bool isSkipNonmarketDate,
        PositionStatus positionStatus,
        string assetClass = "")
    {
        var nowDate = DateTimeOffset.Now;
        return await GetStockPositionValuesAsync(
            upsFilter: upsFilter,
            dateFmDate: nowDate.ToOffset(Config.SystemDateOffset).ToDateOnly(),
            dateToDate: nowDate.ToOffset(Config.SystemDateOffset).ToDateOnly(),
            isSkipNonmarketDate: isSkipNonmarketDate,
            positionStatus: positionStatus,
            assetClass: assetClass);
    }

    public async Task<List<StockPositionValue>> GetStockPositionValuesAsync(
        UPSFilter upsFilter,
        DateOnly dateFmDate,
        DateOnly dateToDate,
        bool isSkipNonmarketDate,
        PositionStatus positionStatus,
        string assetClass = "",
        bool isSkipUnchangedDate = false,
        bool isNotUseCachePos = false)
    {
        var priceCntToCache = (dateToDate.ToDateTime(TimeOnly.MinValue) - dateFmDate.ToDateTime(TimeOnly.MinValue)).Days + 2;

        var gUPS = await context.StockTransactions_ByUPS(upsFilter)
            .Where(t => string.IsNullOrWhiteSpace(assetClass) || t.FkStock.AssetClass == assetClass)
            .Where(t => t.TxDate <= dateToDate)
            .GroupBy(t => new
            {
                t.Uid,
                t.PortfolioId,
                t.StockId,
            })
            .Select(g => new
            {
                g.Key.Uid,
                g.Key.PortfolioId,
                g.Key.StockId,
                g.First().FkStock.StockName, 
                g.First().FkStock.AssetClass,
                g.First().FkStockPortfolio.DefaultCurrency, 
                MinTxDate = g.OrderBy(x => x.TxDate).First().TxDate
            }).LeftJoin(context.StockPositions_ByUPS(upsFilter)
                    .Where(p => !isNotUseCachePos)
                    .Where(p => p.ObserveDate == dateFmDate.AddDays(-1)),
                t => new { t.Uid, t.PortfolioId, t.StockId },
                p => new { p.Uid, p.PortfolioId, p.StockId },
                (t, p) => new
                {
                    t.Uid, 
                    t.PortfolioId, 
                    t.StockId, 
                    t.StockName, 
                    t.AssetClass,
                    t.DefaultCurrency, 
                    t.MinTxDate, 
                    OldStockPos = p,
                    Trans = context.StockTransactions
                        .Where(x => x.Uid == t.Uid)
                        .Where(x => x.PortfolioId == t.PortfolioId)
                        .Where(x => x.StockId == t.StockId)
                        .Where(x => x.TxDate <= dateToDate)
                        //Note that p (ie: oldStockPos) is used
                        //Keep it as N-Query? Query all trans in-memory but 1-Query?
                        .Where(x => p == null || x.TxDate > p.ObserveDate)
                        .OrderBy(x => x.TxDate)
                        .ThenByDescending(x => x.TxCount) //We want to sort by BUY first (we cannot SELL before BUY)
                        .ToList()
                }
            )
            .ToListAsync();

        var stockPositionValues = new List<StockPositionValue>();

        foreach (var portfolioStockId in gUPS)
        {
            var stockPrices2Days = await context.StockPrices
                .Where(p => p.StockId == portfolioStockId.StockId)
                .Where(p => p.MarketDate <= dateToDate)
                .OrderByDescending(p => p.MarketDate)
                .LeftJoin(
                    context.StockDividends,
                    p => new { p.StockId, p.MarketDate },
                    d => new { d.StockId, MarketDate = d.ExDate },
                    (p, pd) => new StockPriceDiv
                    {
                        MarketDate = p.MarketDate,
                        ClosePrice = p.ClosePrice,
                        DivExAmount = pd == null ? null : pd.Amount
                    }
                )
                .Take(priceCntToCache)
                .OrderBy(p => p.MarketDate)
                .ToListAsync();

            var tranIndex = 0;
            var priceIndex = 0;

            var stockPositionValuesPerPortfolioStockId = new List<StockPositionValue>();
            var oldStockPos = portfolioStockId.OldStockPos;

            var latestTranPrice = oldStockPos?.PrevStockPrice ?? 0m; //For Bond that has no stock price in system
            DateOnly? latestTranMarketDate = oldStockPos?.MarketDate;

            var totalStockCount = oldStockPos?.Quantity ?? 0;
            var totalCostAmt = oldStockPos?.TotalCost ?? 0;
            var totalRealisedAmount = oldStockPos?.RealisedAmount ?? 0;
            var totalRealisedDividend = oldStockPos?.RealisedDividend ?? 0;
            var totalRealisedGainExDiv = (oldStockPos?.RealisedGain ?? 0) - totalRealisedDividend;

            var firstTxDate = portfolioStockId.MinTxDate;
            
            var effectiveFmDate = (firstTxDate > dateFmDate ? firstTxDate : dateFmDate);
            
            for (DateOnly curDate = effectiveFmDate; curDate <= dateToDate; curDate = curDate.AddDays(1))
            {
                StockPriceDiv priceCurTxDay = null;
                StockPriceDiv pricePrevTxDay = null;
                var dailyRealisedDividend = 0m;
                var dailyRealisedGainExDiv = 0m;
                var isValueChanged = false;

                if (stockPrices2Days.Count != 0)
                {
                    while (priceIndex + 1 < stockPrices2Days.Count && stockPrices2Days[priceIndex + 1].MarketDate <= curDate)
                    {
                        priceIndex++;
                    }

                    priceCurTxDay = stockPrices2Days[priceIndex];
                    pricePrevTxDay = null;

                    if (isSkipNonmarketDate && priceCurTxDay.MarketDate != curDate)
                    {
                        continue;
                    }

                    if (priceIndex >= 1)
                    {
                        pricePrevTxDay = stockPrices2Days[priceIndex - 1];
                    }
                }

                var val = new StockPositionValue();

                while (tranIndex < portfolioStockId.Trans.Count && portfolioStockId.Trans[tranIndex].TxDate <= curDate)
                {
                    isValueChanged = true;
                    
                    var curTran = portfolioStockId.Trans[tranIndex];
                    if (curTran.TranType is StockTransaction.TRANTYPE_BUY or StockTransaction.TRANTYPE_SELL)
                    {
                        latestTranPrice = curTran.UnitAmt;
                        latestTranMarketDate = curTran.TxDate;
                    }
                    
                    var exRate = CurrencyExchangeRate.GetExRate(curTran.Currency, portfolioStockId.DefaultCurrency);
                    totalCostAmt -= (curTran.HandlingFee.GetValueOrDefault() + curTran.Tax.GetValueOrDefault()) * exRate;
                    
                    switch (curTran.TranType)
                    {
                        case StockTransaction.TRANTYPE_DIV:
                        case StockTransaction.TRANTYPE_CASH:
                            if (curTran.TxDate == curDate)
                            {
                                dailyRealisedDividend += curTran.TxCount * curTran.UnitAmt * exRate;
                            }
                            totalRealisedDividend += curTran.TxCount * curTran.UnitAmt * exRate;
                            break;
                        case StockTransaction.TRANTYPE_BUY:
                            totalStockCount += curTran.TxCount;
                            totalCostAmt += (curTran.UnitAmt * curTran.TxCount) * exRate;
                            
                            //Accrued Interest
                            { 
                                if (curTran.TxDate == curDate)
                                {
                                    dailyRealisedDividend += curTran.AccruedInterest.GetValueOrDefault() * exRate;
                                }
                                totalRealisedDividend += curTran.AccruedInterest.GetValueOrDefault() * exRate;
                            }
                            break;
                        case StockTransaction.TRANTYPE_REINV:
                            //REINV basically mean a div payout then immediate invest
                            //Therefore we basically copy the DIV and BUY part except the fee is joined

                            //Buy Part
                            totalStockCount += curTran.TxCount;
                            totalCostAmt += (curTran.UnitAmt * curTran.TxCount) * exRate;

                            //Cash Part
                            if (curTran.TxDate == curDate)
                            {
                                dailyRealisedDividend += curTran.TxCount * curTran.UnitAmt * exRate;
                            }
                            totalRealisedDividend += curTran.TxCount * curTran.UnitAmt * exRate;
                            break;
                        case StockTransaction.TRANTYPE_SELL:
                            if (totalStockCount < -curTran.TxCount)
                            {
                                throw new InvalidOperationException("Trying to sell more than it is available");
                            }
                            
                            //Accrued Interest
                            {
                                if (curTran.TxDate == curDate)
                                {
                                    dailyRealisedDividend += curTran.AccruedInterest.GetValueOrDefault() * exRate;
                                }
                                totalRealisedDividend += curTran.AccruedInterest.GetValueOrDefault() * exRate;
                            }

                            var realisedAmount = (curTran.UnitAmt * -curTran.TxCount +
                                                 curTran.HandlingFee.GetValueOrDefault() +
                                                 curTran.Tax.GetValueOrDefault()) * exRate;
                            
                            var realisedCost = ((totalCostAmt - totalRealisedAmount) / totalStockCount * -curTran.TxCount) * exRate;
                            totalRealisedAmount += realisedAmount;
                            var realisedGainExDiv = realisedAmount - realisedCost;
                            totalRealisedGainExDiv += realisedGainExDiv;
                            if (curTran.TxDate == curDate)
                            {
                                dailyRealisedGainExDiv += realisedGainExDiv;
                            }

                            totalStockCount -= -curTran.TxCount;
                            break;
                    }

                    tranIndex++;
                }

                // Facts
                val.Uid = portfolioStockId.Uid;
                val.PortfolioId = portfolioStockId.PortfolioId;
                val.StockId = portfolioStockId.StockId;
                val.StockName = portfolioStockId.StockName;
                val.AssetClass = portfolioStockId.AssetClass;
                val.Currency = portfolioStockId.DefaultCurrency ?? Config.DefaultCurrency;
                
                // Running Date info
                val.MarketDate = (priceCurTxDay?.MarketDate ?? DateOnly.MinValue) >= latestTranMarketDate ?
                    priceCurTxDay.MarketDate :
                    latestTranMarketDate;
                val.ObserveDate = curDate; //For example, curDay can be sunday, but market date is Friday
                val.StockPrice = (priceCurTxDay?.MarketDate ?? DateOnly.MinValue) >= latestTranMarketDate ? 
                    priceCurTxDay?.ClosePrice :
                    latestTranPrice;
                
                // Running Sums
                val.Quantity = totalStockCount;
                val.TotalCost = totalCostAmt;
                val.RealisedAmount = totalRealisedAmount;
                val.RealisedGain = totalRealisedGainExDiv + totalRealisedDividend;
                val.RealisedDividend = totalRealisedDividend;
                val.DailyRealisedDividend = dailyRealisedDividend;
                
                // Calculated Fields
                val.RealisedCost = val.RealisedAmount - (val.RealisedGain - val.RealisedDividend);
                val.UnrealisedAmount = val.Quantity * val.StockPrice.GetValueOrDefault();
                val.UnrealisedCost = val.TotalCost - val.RealisedCost;
                val.UnrealisedGain = val.UnrealisedAmount - val.UnrealisedCost;
                val.TotalGain = val.UnrealisedGain + val.RealisedGain;
                val.AverageCost = val.Quantity == 0 ? null : val.UnrealisedCost / val.Quantity;
                
                val.IsLatest = true; //Always true, until the next processed PosVal to flip this value back to false
                
                if (priceCurTxDay == null)
                {
                    val.PrevStockPrice = latestTranPrice;
                    val.CurrentGain = 0;
                    val.CurrentGainPercentage = 0;
                }
                else
                {
                    if (pricePrevTxDay != null)
                    {
                        var divExPrice = priceCurTxDay.DivExAmount ?? 0;
                        val.PrevStockPrice = pricePrevTxDay.ClosePrice;
                        val.CurrentGain = (priceCurTxDay.ClosePrice + divExPrice - pricePrevTxDay.ClosePrice) * val.Quantity;
                        val.CurrentGainPercentage = Utils.GetChangePercentage(pricePrevTxDay.ClosePrice, priceCurTxDay.ClosePrice + divExPrice).GetValueOrDefault();
                    }
                }

                if (isValueChanged || !isSkipUnchangedDate)
                {
                    if (positionStatus == PositionStatus.OpenOrChanged && (val.Quantity > 0 || isValueChanged) ||
                        positionStatus == PositionStatus.Any ||
                        (positionStatus == PositionStatus.Open && val.Quantity > 0) ||
                        (positionStatus == PositionStatus.Closed && val.Quantity <= 0))
                    {
                        stockPositionValues.Add(val);
                        var prevPosVal = stockPositionValuesPerPortfolioStockId.LastOrDefault();
                        if (prevPosVal != null)
                        {
                            prevPosVal.IsLatest = false;
                        }
                        stockPositionValuesPerPortfolioStockId.Add(val);
                    }
                } 
            }
        }

        return stockPositionValues;
    }

    public async Task UpdateStockPositionAsync(
        UPSFilter upsFilter,
        DateOnly? fromDate = null,
        bool isNotUseCachePos = false
        )
    {
        var nowDate = DateTimeOffset.Now;
        var fromDateTruncated = fromDate ?? DateTimeOffset.FromUnixTimeSeconds(0).ToOffset(Config.SystemDateOffset).ToDateOnly();
        var toDateTruncated = nowDate.ToOffset(Config.SystemDateOffset).ToDateOnly();

        var earliestLatestStockPos = await context
            .StockPositions_ByUPS(upsFilter)
            .Where(p => p.IsLatest)
            .MinAsync(p => (DateOnly?)p.ObserveDate); //Cast so it returns null if empty set

        var dateFmGenerate = new[] { earliestLatestStockPos ?? DateOnly.MaxValue, fromDateTruncated }.Min(); 
        
        var allStocksPositions = await GetStockPositionValuesAsync(
            upsFilter, 
            dateFmDate: dateFmGenerate, 
            dateToDate: toDateTruncated,
            isSkipNonmarketDate: false,
            PositionStatus.OpenOrChanged,
            isSkipUnchangedDate: false,
            isNotUseCachePos: isNotUseCachePos);
        
        var existingPositionsMap = await context
            .StockPositions_ByUPS(upsFilter)
            .Where(p => p.ObserveDate >= dateFmGenerate)
            .ToDictionaryAsync(p => (p.Uid, p.PortfolioId, p.StockId, p.ObserveDate));
        
        foreach (var newPos in allStocksPositions)
        {
            var key = (newPos.Uid, newPos.PortfolioId, newPos.StockId, newPos.ObserveDate);

            if (existingPositionsMap.TryGetValue(key, out var existingEntity))
            {
                // UPDATE: Copies updated properties to the existing tracked entity
                context.Entry(existingEntity).CurrentValues.SetValues(newPos);
                existingPositionsMap.Remove(key); // Mark as retained
            }
            else
            {
                // INSERT: Add non-existent record
                context.StockPositions.Add(newPos);
            }
        }
        
        // Cleanup stale records (positions that were deleted/invalidated during recalculation)
        context.StockPositions.RemoveRange(existingPositionsMap.Values);
        
        await context.SaveChangesAsync();
    }
    
    public static IEnumerable<StockPositionValue> GroupStockPositionValueByStockId(IEnumerable<StockPositionValue> stockPositionValues)
    {
        var rtv = from v in stockPositionValues
            group v by new
            {
                StockId = v.StockId,
                StockName = v.StockName,
                Currency = v.Currency,
                ObserveDate = v.ObserveDate,
            } into g
            select new StockPositionValue()
            {
                PortfolioId = "",
                StockId = g.Key.StockId,
                StockName = g.Key.StockName,
                AssetClass = g.First().AssetClass,
                Currency = g.Key.Currency,
                MarketDate = g.Max(v => v.MarketDate),
                ObserveDate = g.Key.ObserveDate,
                StockPrice = g.Max(v => v.StockPrice),
                PrevStockPrice = g.Max(v => v.PrevStockPrice),
                TotalCost = g.Sum(v => v.TotalCost),
                Quantity = g.Sum(v => v.Quantity),
                CurrentGain = g.Sum(v => v.CurrentGain),
                CurrentGainPercentage = g.Max(v => v.CurrentGainPercentage),
                RealisedAmount = g.Sum(v => v.RealisedAmount),
                RealisedGain = g.Sum(v => v.RealisedGain),
                RealisedDividend = g.Sum(v => v.RealisedDividend),
                RealisedCost = g.Sum(v => v.RealisedAmount - (v.RealisedGain - v.RealisedDividend)),
                UnrealisedAmount = g.Sum(v => v.Quantity * v.StockPrice.GetValueOrDefault()),
                UnrealisedCost = g.Sum(v => v.TotalCost - v.RealisedCost),
                UnrealisedGain = g.Sum(v => v.UnrealisedAmount - v.UnrealisedCost),
                TotalGain = g.Sum(v => v.UnrealisedGain + v.RealisedGain),
                AverageCost = g.Sum(v => v.Quantity) == 0 ? null : 
                    g.Sum(v => v.UnrealisedCost) / g.Sum(v => v.Quantity),
            };
        return rtv;
    }
}