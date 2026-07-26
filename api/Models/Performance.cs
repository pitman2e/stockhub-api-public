using StockHub.Database;

namespace StockHub.Models;

public class Performance
{
    public Stock Stock { get; set; }
    public decimal? YTD { get; set; }
    public decimal? OneYear { get; set; }
    public decimal? ThreeYear { get; set; }
    public decimal? FiveYear { get; set; }
    public decimal? OneMonth { get; set; }
    public decimal? ThreeMonth { get; set; }
    public decimal? DropFromTop { get; set; }
}