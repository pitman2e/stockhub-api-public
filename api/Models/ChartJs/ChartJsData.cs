using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockHub.Models.ChartJs;

public class ChartJsDataSets
{
    [JsonPropertyName("datasets")]
    public List<ChartJsDataSet> Datasets { get; set; } = new();

    [JsonPropertyName("labels")]
    public List<string> Labels { get; set; } = new();
}