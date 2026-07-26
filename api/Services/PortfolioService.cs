using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockHub.Controllers.Portfolio;
using StockHub.Database;
using StockHub.Errors;
using StockHub.Exchanges;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Extensions;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Models.ChartJs;
using StockHub.Services.Position;
using StockHub.Tools;

namespace StockHub.Services;

public class PortfolioService(
    StockHubContext context,
    PositionValueService positionValueService,
    Stock2ExchangeService stock2ExchangeService,
    IUserClaims userClaims)
{
    public async Task<PortfoliosSummary> GetSummaryAsync(StockPortfolio? portfolio, string displayCurrency = Config.DefaultCurrency)
    {
        if (!CurrencyExchangeRate.IsValidOrEmpty(displayCurrency))
        {
            throw new SHArgumentException($"Display currency '{displayCurrency}' is not supported");
        }

        var portSummary = new PortfoliosSummary();
        var rtvAllDetails = new List<StockSummary>();

        var dictVirtualPortfolio = new Dictionary<string, StockSummary>();

        var upsFilter = UPSFilter.GetFilter(userClaims.GetUid(), portfolio?.PortfolioId);

        var portfolios = await (from p in context.StockPortfolios
                         .ByUid(upsFilter.Uid)
                         .ByPortfolioId_All(upsFilter.PortfolioId, isNullable: upsFilter.NullablePortfolioId)
                         .Include(p => p.FkStockVirtualChildPortfolios)
                          orderby p.Priority
                          select p)
                         .ToListAsync();

        var positionValues =
            from p in context.StockPositions_ByUPS(upsFilter)
            where p.IsLatest
            select p;
        
        var yearStartPositionValues =
            from p in context.StockPositions_ByUPS(upsFilter)
            where p.ObserveDate == new DateOnly(positionValues.Max(pv => pv.ObserveDate).Year, 1, 1) ||
                  (p.IsLatest && p.Quantity == 0)
            select p;
        
        var maxMarketDate = await positionValues
                            .Where(p => p.UnrealisedAmount > 0)
                            .MaxAsync(t => t.MarketDate);

        foreach (var nonVirPortfolio in portfolios.Where(p => !p.IsVirtual))
        {
            var pPositionValue = await positionValues
                .Where(p => p.Uid ==  nonVirPortfolio.Uid)
                .Where(p => p.PortfolioId == nonVirPortfolio.PortfolioId)
                .ToListAsync();
            
            // TODO: Better pYearStartPositionValues duplicate record fix 
            // pYearStartPositionValues will contain duplicate record if a stock
            // is all sold between the start the year and NOW
            // The variable dupAdjustedTotalGain is use to workaround this by substracting the duplicate stock record
            // Duplicate observe dates are year start date and now date (marked by latest)
            var pYearStartPositionValues = await yearStartPositionValues
                .Where(p => p.Uid ==  nonVirPortfolio.Uid)
                .Where(p => p.PortfolioId == nonVirPortfolio.PortfolioId)
                .OrderBy(p => p.StockId)
                .ThenBy(p => p.ObserveDate)
                .ToListAsync();

            string prevStockId = null;
            decimal dupAdjustedTotalGain = 0;
            foreach (var p in pYearStartPositionValues)
            {
                if (p.StockId == prevStockId)
                {
                    dupAdjustedTotalGain += p.TotalGain;
                }
                prevStockId = p.StockId;
            }
            
            var detail = new StockSummary
            {
                PortfolioId = nonVirPortfolio.PortfolioId,
                PortfolioName = nonVirPortfolio.Name,
                IsExcludedFromSummary = nonVirPortfolio.IsExcludedFromSummary
            };
            detail.MarketDate = new[] { detail.MarketDate, pPositionValue.Max(u => u.MarketDate) }.Max();
            detail.PortfolioCurrency = nonVirPortfolio.DefaultCurrency;
            detail.DisplayCurrency = displayCurrency.IsNullOrWhiteSpaceThen(nonVirPortfolio.DefaultCurrency);
            detail.Version = nonVirPortfolio.Version;

            //----Money calculation
            var exRate = CurrencyExchangeRate.GetExRate(detail.PortfolioCurrency, detail.DisplayCurrency);
            detail.CurTxGainAmount = pPositionValue.Sum(t => t.CurrentGain) * exRate;
            detail.CurTxGainAmountLatest = pPositionValue.Where(t => t.MarketDate == maxMarketDate).Sum(t => t.CurrentGain) * exRate;
            detail.TotalUnrealisedAmountPrev = pPositionValue.Sum(t => t.Quantity * t.PrevStockPrice.GetValueOrDefault()) * exRate;
            detail.TotalCost = pPositionValue.Sum(t => t.TotalCost) * exRate;
            detail.TotalDividend = pPositionValue.Sum(t => t.RealisedDividend) * exRate;
            detail.TotalRealisedAmount = pPositionValue.Sum(t => t.RealisedAmount) * exRate;
            detail.TotalRealisedCost = pPositionValue.Sum(t => t.RealisedCost) * exRate;
            detail.TotalRealisedGain = pPositionValue.Sum(t => t.RealisedGain) * exRate;
            detail.TotalUnrealisedAmount = pPositionValue.Sum(t => t.UnrealisedAmount) * exRate;
            detail.TotalUnrealisedGain = pPositionValue.Sum(t => t.UnrealisedGain) * exRate;
            detail.TotalUnrealisedCost = pPositionValue.Sum(t => t.UnrealisedCost) * exRate;
            detail.TotalYtdGain = (pPositionValue.Sum(t => t.TotalGain) - pYearStartPositionValues.Sum(t => t.TotalGain) + dupAdjustedTotalGain) * exRate;

            if (detail.TotalUnrealisedAmount > 0)
            {
                portSummary.Details.Add(detail);
            }
            else
            {
                portSummary.ClosedDetails.Add(detail);
            }
            rtvAllDetails.Add(detail);

            foreach (var virPortfolio in nonVirPortfolio.FkStockVirtualChildPortfolios)
            {
                if (!dictVirtualPortfolio.TryGetValue(virPortfolio.PortfolioId, out var vpSummary))
                {
                    vpSummary = new StockSummary();
                    var linkingVirPortfolio = context.StockPortfolios.First(p => p.PortfolioId == virPortfolio.PortfolioId);
                    vpSummary.PortfolioId = linkingVirPortfolio.PortfolioId;
                    vpSummary.PortfolioCurrency = linkingVirPortfolio.DefaultCurrency;
                    vpSummary.DisplayCurrency = string.IsNullOrWhiteSpace(displayCurrency) ? linkingVirPortfolio.DefaultCurrency : displayCurrency;
                    vpSummary.PortfolioName = linkingVirPortfolio.Name;
                    vpSummary.IsVirtual = true;
                    vpSummary.Version = linkingVirPortfolio.Version;
                    dictVirtualPortfolio[linkingVirPortfolio.PortfolioId] = vpSummary;
                }

                var vpExRate = CurrencyExchangeRate.GetExRate(detail.DisplayCurrency, vpSummary.DisplayCurrency);
                vpSummary.MarketDate = new[] { detail.MarketDate, vpSummary.MarketDate }.Max();
                vpSummary.CurTxGainAmount += detail.CurTxGainAmount * vpExRate;
                vpSummary.CurTxGainAmountLatest += detail.CurTxGainAmountLatest * vpExRate;
                vpSummary.TotalUnrealisedAmountPrev += detail.TotalUnrealisedAmountPrev * vpExRate;
                vpSummary.TotalCost += detail.TotalCost * vpExRate;
                vpSummary.TotalDividend += detail.TotalDividend * vpExRate;
                vpSummary.TotalRealisedAmount += detail.TotalRealisedAmount * vpExRate;
                vpSummary.TotalRealisedCost += detail.TotalRealisedCost * vpExRate;
                vpSummary.TotalRealisedGain += detail.TotalRealisedGain * vpExRate;
                vpSummary.TotalUnrealisedAmount += detail.TotalUnrealisedAmount * vpExRate;
                vpSummary.TotalUnrealisedGain += detail.TotalUnrealisedGain * vpExRate;
                vpSummary.TotalUnrealisedCost += detail.TotalUnrealisedCost * vpExRate;
                vpSummary.TotalYtdGain += detail.TotalYtdGain * vpExRate;
            }
        }
        portSummary.VirtualPortfolioDetails = dictVirtualPortfolio.Values.ToList();

        var rtvDetailsInc = rtvAllDetails.Where(d => !d.IsExcludedFromSummary).ToList();

        portSummary.Summary.MarketDate = rtvDetailsInc.Count != 0 ? rtvDetailsInc.Max(u => u.MarketDate) : null;
        portSummary.Summary.PortfolioCurrency = displayCurrency.IsNullOrWhiteSpaceThen(Config.DefaultCurrency);
        portSummary.Summary.DisplayCurrency = displayCurrency.IsNullOrWhiteSpaceThen(Config.DefaultCurrency);
        portSummary.Summary.PortfolioId = "Summary";
        portSummary.Summary.PortfolioName = "Summary";
        portSummary.Summary.Version = 0;
        portSummary.Summary.CurTxGainAmount = rtvDetailsInc.Sum(s => s.CurTxGainAmount * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.CurTxGainAmountLatest = rtvDetailsInc.Sum(s => s.CurTxGainAmountLatest * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalUnrealisedAmountPrev = rtvDetailsInc.Sum(s => s.TotalUnrealisedAmountPrev * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalCost = rtvDetailsInc.Sum(s => s.TotalCost * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalDividend = rtvDetailsInc.Sum(s => s.TotalDividend * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalRealisedAmount = rtvDetailsInc.Sum(s => s.TotalRealisedAmount * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalRealisedCost = rtvDetailsInc.Sum(s => s.TotalRealisedCost * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalRealisedGain = rtvDetailsInc.Sum(s => s.TotalRealisedGain * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalUnrealisedAmount = rtvDetailsInc.Sum(s => s.TotalUnrealisedAmount * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalUnrealisedGain = rtvDetailsInc.Sum(s => s.TotalUnrealisedGain * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalUnrealisedCost = rtvDetailsInc.Sum(s => s.TotalUnrealisedCost * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));
        portSummary.Summary.TotalYtdGain = rtvDetailsInc.Sum(s => s.TotalYtdGain * CurrencyExchangeRate.GetExRate(s.DisplayCurrency, displayCurrency));

        return portSummary;
    }

    public async Task<IEnumerable<StockPositionValue>> GetGroupedLatestPositionsAsync(
        UPSFilter upsFilter,
        bool groupByStockId,
        PositionValueService.PositionStatus positionStatus)
    {
        var positions = await positionValueService.GetLatestPositionsValueAsync(
            upsFilter,
            isSkipNonmarketDate: false,
            positionStatus: positionStatus);
        if (groupByStockId)
        {
            return PositionValueService.GroupStockPositionValueByStockId(positions);
        }

        return positions;
    }

    public async Task<ChartJsDataSets> GetPieChartDataAsync(
        UPSFilter upsFilter, 
        string tagCategory = "", 
        string assetClass = "")
    {
        var positions = await positionValueService.GetLatestPositionsValueAsync(
            upsFilter,
            isSkipNonmarketDate: false,
            assetClass: assetClass,
            positionStatus: PositionValueService.PositionStatus.Open);

        var dictValue = new Dictionary<string, decimal>();
        var dictLabels = new Dictionary<string, string>();
        var dictColor = new Dictionary<string, string>();
        var rtv = new ChartJsDataSets();
        rtv.Datasets.Add(new ChartJsDataSet());

        var distinctCurrencies = positions.Select(u => u.Currency).Distinct().ToList();
        var displayCurrency = distinctCurrencies.Count == 1 ? distinctCurrencies.First() : Config.DefaultCurrency;

        switch ((tagCategory + "").Trim().ToUpperInvariant())
        {
            case "":
                foreach (var gStock in positions.GroupBy(u => new { u.StockId, u.StockName }))
                {
                    var unrealVal =
                        positions.Where(u => u.StockId == gStock.Key.StockId)
                                        .Sum(g => g.UnrealisedAmount *
                                            CurrencyExchangeRate.GetExRate(g.Currency, displayCurrency)
                                            );

                    var stock = stock2ExchangeService.ParseExact(gStock.Key.StockId);
                    var labelText = stock.Exchange.MarketId == HK.MARKET_ID ? gStock.Key.StockName : gStock.Key.StockId;
                    dictValue.TryAdd(gStock.Key.StockId, unrealVal);
                    dictLabels.TryAdd(gStock.Key.StockId, labelText);
                    dictColor.TryAdd(gStock.Key.StockId, "");
                }
                break;
            default:
                foreach (var gStock in positions.GroupBy(u => new { u.StockId, u.StockName }))
                {
                    IEnumerable<StockTag> stockTags;
                    if (tagCategory == "CLASS")
                    {
                        var distinctStocks = gStock.Select(s => s.StockId).Distinct();

                        stockTags = await (from s in context.Stocks
                            where distinctStocks.Contains(s.StockId)
                            select new StockTag
                            {
                                Uid = userClaims.GetUid(),
                                StockId = s.StockId,
                                TagCategory = tagCategory,
                                Tag = s.AssetClass,
                                Percentage = 100,
                            }).ToListAsync();
                    }
                    else
                    {
                        stockTags = await context.StockTags
                            .ByUid(userClaims.GetUid())
                            .ByStockId(gStock.Key.StockId)
                            .Where(t => t.TagCategory == tagCategory)
                            .ToListAsync();
                    }

                    var unrealVal = positions
                        .Where(u => u.StockId == gStock.Key.StockId)
                        .Sum(g => g.UnrealisedAmount * CurrencyExchangeRate.GetExRate(g.Currency, displayCurrency));

                    var otherPercentage = 100m;
                    foreach (var stockTag in stockTags)
                    {
                        if (dictValue.TryAdd(stockTag.Tag, 0))
                        {
                            dictColor.TryAdd(stockTag.Tag, stockTag.Color);
                        }
                        dictValue[stockTag.Tag] += unrealVal * stockTag.Percentage / 100;
                        otherPercentage -= stockTag.Percentage;
                    }

                    if (otherPercentage > 0)
                    {
                        dictValue.TryAdd("Other", 0);
                        dictValue["Other"] += unrealVal * otherPercentage / 100;
                        dictColor.TryAdd("Other", "");
                    }
                }
                break;
        }

        var keysSortedByVal = from t in dictValue
                              orderby t.Value descending
                              select t;

        foreach (var keyVal in keysSortedByVal)
        {
            rtv.Datasets[0].Data.Add(keyVal.Value);
            rtv.Datasets[0].Currency = displayCurrency;
            rtv.Datasets[0].CustomBackgroundColor.Add(dictColor[keyVal.Key]);
            rtv.Labels.Add(keyVal.Key);
        }

        return rtv;
    }
}