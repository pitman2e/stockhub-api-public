using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using StockHub.Errors;
using StockHub.Models.ChartJs;

namespace StockHub.Models;

public class PositionChartData
{
    private PositionChartData()
    {
    }

    public PositionChartData(List<StockPositionValue> stockPositionValues, int dayRes = 1)
    {
        if (dayRes != 1 && dayRes != 7 && dayRes != 30)
        {
            throw new SHArgumentException("Day Res must be 1D/1W/1M");
        }

        UnrealisedDatasets = new List<ChartJsDataSet>();
        UnrealisedCostDatasets = new List<ChartJsDataSet>();
        TotalGainDatasets = new List<ChartJsDataSet>();
        TotalGainOffsetDatasets = new List<ChartJsDataSet>();
        DailyGainDatasets = new List<ChartJsDataSet>();
        DailyRealisedDividendDatasets = new List<ChartJsDataSet>();
        Labels = new List<string>();

        var currencies = stockPositionValues
            .Select(s => s.Currency)
            .Distinct()
            .ToList();
        
        var currency = currencies.Count == 1 ? currencies.First() : Config.DefaultCurrency;

        var stocksGrp = stockPositionValues
                        .GroupBy(s => new
                        {
                            ObserveDate = s.ObserveDate
                        })
                        .Select(g2 => new
                        {
                            ObserveDate = g2.Key.ObserveDate,
                            IsTradingDay = g2.Max(t => t.IsTradingDay),
                            UnrealisedValue = g2.Sum(t => t.UnrealisedAmount * CurrencyExchangeRate.GetExRate(t.Currency, currency)),
                            UnrealisedCost = g2.Sum(t => t.UnrealisedCost * CurrencyExchangeRate.GetExRate(t.Currency, currency)),
                            DailyRealisedDividend = g2.Sum(t => t.DailyRealisedDividend * CurrencyExchangeRate.GetExRate(t.Currency, currency)),
                            TotalGain = g2.Sum(t => t.TotalGain * CurrencyExchangeRate.GetExRate(t.Currency, currency)),
                            DailyGain = g2.Sum(t => !t.IsTradingDay ? 0 : t.CurrentGain * CurrencyExchangeRate.GetExRate(t.Currency, currency)),
                        });

        if (dayRes == 7)
        {
            stocksGrp = stocksGrp
            .GroupBy(s => new
            {
                ObserveDate = s.ObserveDate.AddDays(-(int)s.ObserveDate.DayOfWeek)
            })
            .Select(g2 => new
            {
                ObserveDate = g2.Key.ObserveDate,
                IsTradingDay = true,
                UnrealisedValue = g2.Average(t => t.UnrealisedValue),
                UnrealisedCost = g2.Average(t => t.UnrealisedCost),
                DailyRealisedDividend = g2.Sum(t => t.DailyRealisedDividend),
                TotalGain = g2.Average(t => t.TotalGain),
                DailyGain = g2.Sum(t => t.DailyGain),
            });
        };

        if (dayRes == 30)
        {
            stocksGrp = stocksGrp
            .GroupBy(s => new
            {
                ObserveDate = s.ObserveDate.AddDays(-s.ObserveDate.Day + 1)
            })
            .Select(g2 => new
            {
                ObserveDate = g2.Key.ObserveDate,
                IsTradingDay = true,
                UnrealisedValue = g2.Average(t => t.UnrealisedValue),
                UnrealisedCost = g2.Average(t => t.UnrealisedCost),
                DailyRealisedDividend = g2.Sum(t => t.DailyRealisedDividend),
                TotalGain = g2.Average(t => t.TotalGain),
                DailyGain = g2.Sum(t => t.DailyGain),
            });
        };

        stocksGrp = stocksGrp
            .OrderBy(g2 => g2.ObserveDate)
            .ToList();

        var firstObsDate = stockPositionValues.FirstOrDefault()?.ObserveDate;

        var dsUnrealisedValue = new ChartJsDataSet() { Currency = currency };
        var dsUnrealisedCost = new ChartJsDataSet() { Currency = currency };
        var dsTotalGain = new ChartJsDataSet() { Currency = currency };
        var dsTotalGainOffset = new ChartJsDataSet() { Currency = currency };
        var dsDailyGain = new ChartJsDataSet() { Currency = currency };
        var dsDailyRealisedDiv = new ChartJsDataSet() { Currency = currency };

        UnrealisedDatasets.Add(dsUnrealisedValue);
        UnrealisedCostDatasets.Add(dsUnrealisedCost);
        TotalGainDatasets.Add(dsTotalGain);
        TotalGainOffsetDatasets.Add(dsTotalGainOffset);
        DailyGainDatasets.Add(dsDailyGain);
        DailyRealisedDividendDatasets.Add(dsDailyRealisedDiv);

        if (firstObsDate == null)
        {
            return;
        }

        string GetLabel(DateOnly dt)
        {
            switch (dayRes)
            {
                case 30:
                    return dt.ToString("yyyy-MM");
                default:
                    return dt.ToString("yyyy-MM-dd");
            }
        }

        var stockFirstObsDate = stocksGrp.First().ObserveDate;
        for (var dt = firstObsDate.Value; dt < stockFirstObsDate; dt = dt.AddDays(1))
        {
            if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
            {
                Labels.Add(GetLabel(dt));
                dsUnrealisedValue.Data.Add(0);
                dsUnrealisedCost.Data.Add(0);
                dsTotalGain.Data.Add(0);
                dsDailyGain.Data.Add(0);
                dsDailyRealisedDiv.Data.Add(0);
            }
        }

        foreach (var stockValues in stocksGrp)
        {
            if (stockValues.IsTradingDay)
            {
                Labels.Add(GetLabel(stockValues.ObserveDate));
                dsUnrealisedValue.Data.Add(stockValues.UnrealisedValue);
                dsUnrealisedCost.Data.Add(stockValues.UnrealisedCost);
                dsTotalGain.Data.Add(stockValues.TotalGain);
                dsDailyGain.Data.Add(stockValues.DailyGain);
                dsDailyRealisedDiv.Data.Add(stockValues.DailyRealisedDividend);
            }
        }

        dsTotalGainOffset.Data = dsTotalGain.Data.Select(d => d - dsTotalGain.Data[0] + dsDailyGain.Data[0]).ToList();
    }

    [JsonPropertyName("unrealisedDatasets")]
    public List<ChartJsDataSet> UnrealisedDatasets { get; }
    
    [JsonPropertyName("unrealisedCostDatasets")]
    public List<ChartJsDataSet> UnrealisedCostDatasets { get; }
    
    [JsonPropertyName("totalGainDatasets")]
    public List<ChartJsDataSet> TotalGainDatasets { get; }
    
    [JsonPropertyName("totalGainOffsetDatasets")]
    public List<ChartJsDataSet> TotalGainOffsetDatasets { get; }

    [JsonPropertyName("dailyGainDatasets")]
    public List<ChartJsDataSet> DailyGainDatasets { get; }
    
    [JsonPropertyName("dailyRealisedDividendDatasets")]
    public List<ChartJsDataSet> DailyRealisedDividendDatasets { get; }

    [JsonPropertyName("labels")]
    public List<string> Labels { get; }
}