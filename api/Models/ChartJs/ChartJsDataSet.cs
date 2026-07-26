using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockHub.Models.ChartJs;

public class ChartJsDataSet
{
    [JsonPropertyName("data")]
    public List<decimal> Data { get; set; } = new();

    //If prop name is backgroundColor, the Chart.JS lib will use the color directly
    [JsonPropertyName("customBackgroundColor")]
    public List<string> CustomBackgroundColor { get; set; } = new();

    [JsonPropertyName("currency")]
    public string Currency { get; set; }
}