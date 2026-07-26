using System.Collections.Generic;

namespace StockHub.Models;

public class PortfoliosSummary
{
    public StockSummary Summary { get; set; }
    public List<StockSummary> Details  { get; set; }
    public List<StockSummary> ClosedDetails  { get; set; }
    public List<StockSummary> VirtualPortfolioDetails  { get; set; }

    public PortfoliosSummary()
    {
        Summary = new StockSummary();
        Details = new List<StockSummary>();
        ClosedDetails = new List<StockSummary>();
        VirtualPortfolioDetails = new List<StockSummary>();
    }
}
