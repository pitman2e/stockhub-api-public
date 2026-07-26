using System;
using System.Collections.Generic;
using System.Linq;
using StockHub.Database;
using StockHub.Extensions;
using StockHub.Models;
using StockHub.Interfaces;
using StockHub.Repositories;
using StockHub.Tools;

namespace StockHub.Services.Position;

public class PositionValueServiceV1(
    StockHubContext context,
    TransactionRepo transactionRepo)
{
    public List<StockPositionValue> GetStockPositionValues(
        UPSFilter upsFilter,
        DateOnly dateFmDate,
        DateOnly dateToDate,
        bool isSkipNonmarketDate,
        PositionValueService.PositionStatus positionStatus,
        bool isSkipUnchangedDate = false)
    {
        var allStocksTrans = transactionRepo.Get(upsFilter, dateToDate: dateToDate);
        var allStockIds = allStocksTrans.Select(s => s.StockId).Distinct().ToList();

        var priceCntToCache = (dateToDate.ToDateTime(TimeOnly.MinValue) - dateFmDate.ToDateTime(TimeOnly.MinValue)).Days + 2;

        var precacheStockExDividends =
                            context.StockDividends
                            .Where(s => allStockIds.Contains(s.StockId))
                            .Where(s => dateFmDate <= s.ExDate)
                            .Where(s => dateToDate >= s.ExDate)
                            .Select(s => s)
                            .ToList();

        var stockPositionValues = new List<StockPositionValue>();

        var portfolioStockIds =
        from t in allStocksTrans
        group t by new
        {
            t.Uid,
            t.PortfolioId,
            t.StockId,
            t.FkStock.StockName,
            t.FkStock.AssetClass,
            PortfolioCurrency = t.FkStockPortfolio.DefaultCurrency,
        } into g
        select new
        {
            Keys = g.Key,
            Details = g
            .OrderBy(s => s.TxDate)
            .ThenByDescending(s => s.TxCount) //We want to sort by BUY first (we cannot SELL before BUY)
            .ToList()
        };

        foreach (var portfolioStockId in portfolioStockIds)
        {
            var stockPrices2Days = (
                    from p in context.StockPrices
                    where p.StockId == portfolioStockId.Keys.StockId
                    where p.MarketDate <= dateToDate
                    orderby p.MarketDate descending 
                    select p)
                .Take(priceCntToCache)
                .OrderBy(p => p.MarketDate)
                .ToList();

            var totalStockCount = 0m;
            var totalCostAmt = 0m;
            var totalRealisedAmount = 0m;
            var totalRealisedDividend = 0m;
            var totalRealisedGainExDiv = 0m;

            var tranIndex = 0;
            var priceIndex = 0;

            var stockPositionValuesPerPortfolioStockId = new List<StockPositionValue>();
            
            var latestTranPrice = 0m; //For Bond that has no stock price in system
            DateOnly? latestTranDate = null;

            var firstTxDate = allStocksTrans
                .Where(t => t.PortfolioId == portfolioStockId.Keys.PortfolioId)
                .Where(t => t.StockId == portfolioStockId.Keys.StockId)
                .Min(t => t.TxDate);
            
            var effectiveFmDate = (firstTxDate > dateFmDate ?  firstTxDate : dateFmDate);
            
            for (DateOnly curDate = effectiveFmDate; curDate <= dateToDate; curDate = curDate.AddDays(1))
            {
                StockPrice priceCurTxDay = null;
                StockPrice pricePrevTxDay = null;
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

                while (tranIndex < portfolioStockId.Details.Count && portfolioStockId.Details[tranIndex].TxDate <= curDate)
                {
                    isValueChanged = true;
                    
                    var curTran = portfolioStockId.Details[tranIndex];
                    if (curTran.TranType is StockTransaction.TRANTYPE_BUY or StockTransaction.TRANTYPE_SELL
                    )
                    {
                        latestTranPrice = curTran.UnitAmt;
                        latestTranDate = curTran.TxDate;
                    }
                    
                    var exRate = CurrencyExchangeRate.GetExRate(curTran.Currency, portfolioStockId.Keys.PortfolioCurrency);
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

                val.Uid = portfolioStockId.Keys.Uid;
                val.PortfolioId = portfolioStockId.Keys.PortfolioId;
                val.StockId = portfolioStockId.Keys.StockId;
                val.StockName = portfolioStockId.Keys.StockName;
                val.AssetClass = portfolioStockId.Keys.AssetClass;
                val.Currency = portfolioStockId.Keys.PortfolioCurrency ?? Config.DefaultCurrency;
                val.MarketDate = priceCurTxDay?.MarketDate ?? latestTranDate;
                val.ObserveDate = curDate; //For example, curDay can be sunday, but market date is Friday
                val.TotalCost = totalCostAmt;
                val.Quantity = totalStockCount;
                val.StockPrice = priceCurTxDay?.ClosePrice ?? latestTranPrice;
                val.RealisedAmount = totalRealisedAmount;
                val.RealisedGain = totalRealisedGainExDiv + totalRealisedDividend;
                val.RealisedDividend = totalRealisedDividend;
                val.DailyRealisedDividend = dailyRealisedDividend;
                val.IsLatest = true; //Always true, until the next processed PosVal to flip this value back to false
                val.RealisedCost = val.RealisedAmount - (val.RealisedGain - val.RealisedDividend);
                val.UnrealisedAmount = val.Quantity * val.StockPrice.GetValueOrDefault();
                val.UnrealisedCost = val.TotalCost - val.RealisedCost;
                val.UnrealisedGain = val.UnrealisedAmount - val.UnrealisedCost;
                val.TotalGain = val.UnrealisedGain + val.RealisedGain;
                val.AverageCost = val.Quantity == 0 ? null : val.UnrealisedCost / val.Quantity;

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
                        var divExPrice = precacheStockExDividends
                                        .FirstOrDefault(d => d.StockId == val.StockId && d.ExDate == val.MarketDate)
                                        ?.Amount ?? 0;

                        val.PrevStockPrice = pricePrevTxDay.ClosePrice;
                        val.CurrentGain = (priceCurTxDay.ClosePrice + divExPrice - pricePrevTxDay.ClosePrice) * val.Quantity;
                        val.CurrentGainPercentage = Utils.GetChangePercentage(pricePrevTxDay.ClosePrice, priceCurTxDay.ClosePrice + divExPrice).GetValueOrDefault();
                    }
                }

                if (isValueChanged || !isSkipUnchangedDate)
                {
                    if (positionStatus == PositionValueService.PositionStatus.OpenOrChanged && (val.Quantity > 0 || isValueChanged) ||
                        positionStatus == PositionValueService.PositionStatus.Any ||
                        (positionStatus == PositionValueService.PositionStatus.Open && val.Quantity > 0) ||
                        (positionStatus == PositionValueService.PositionStatus.Closed && val.Quantity <= 0))
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
}