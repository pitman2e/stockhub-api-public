using System.Collections.Generic;
using System.Text.Json.Serialization;
using StockHub.Database;

namespace StockHub.Models;

public class StockPriceValueData
{
    private StockPriceValueData()
    {
    }

    public StockPriceValueData(IEnumerable<StockPrice> stockPrices)
    {
        StockPriceDatasets = new List<DataSet>();
        Labels = new List<string>();

        var dsPrice = new DataSet();

        StockPriceDatasets.Add(dsPrice);
        foreach (var stockValues in stockPrices)
        {
            dsPrice.Label = "";
            Labels.Add(stockValues.MarketDate.ToString("yyyy-MM-dd"));
            dsPrice.Data.Add(stockValues.ClosePrice);
        }
    }

    public class DataSet
    {
        public DataSet()
        {
            Data = new List<decimal>();
        }

        [JsonPropertyName("label")]
        public string Label { get; set; }
        
        [JsonPropertyName("data")]
        public List<decimal> Data { get; set; }
        
        [JsonPropertyName("fill")]
        public bool Fill { get; set; } = true;
    }

    [JsonPropertyName("stockPriceDatasets")]
    public List<DataSet> StockPriceDatasets { get; set; }

    
    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; }
}