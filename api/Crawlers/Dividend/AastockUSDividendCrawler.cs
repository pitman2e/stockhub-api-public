using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using Microsoft.Extensions.Logging;
using StockHub.Database;
using StockHub.Models;

namespace StockHub.Crawlers.Dividend;

public class AastockUSDividendCrawler(
    HttpClient httpClient,
    ILogger<AastockUSDividendCrawler> logger)
    : IAastockUSDividendCrawler
{

    public async Task<IEnumerable<StockDividend>> CrawlAsync(StockAdapter stock)
    {
        var config = Configuration.Default.WithDefaultLoader();
        var browsingContext = BrowsingContext.New(config);
        var aastockId = AastocksStockIdMapper.Map(stock);
        var document = await browsingContext.OpenAsync($"http://www.aa" +
                                                       $"sto" +
                                                       $"cks.com/tc/usq/analysis/dividend.aspx?symbol={aastockId}");

        {
            var selector = "table.cnhk-cf>tbody>tr";
            var trs = document.QuerySelectorAll(selector);

            /* AAPL (Col count = 6)
            <td class="mcFont nowrap cls">2020/04/30</td> --0
            <td class="mcFont txt_r nowrap cls">2020/09</td> --1
            <td style="padding-left:20px" class="mcFont txt_l cls">USD 0.205</td> --2
            <td class="mcFont txt_r nowrap cls">2020/05/08</td> --3
            <td class="mcFont txt_r nowrap cls">2020/05/11</td> --4
            <td class="mcFont txt_r nowrap cls">2020/05/14</td> --5
            */

            /* VOO (Col count = 5)
            <td class="mcFont nowrap cls">2020/03/06</td> --0
            <td style="padding-left:20px" class="mcFont txt_l cls">USD 1.178</td> --1
            <td class="mcFont txt_r nowrap cls">2020/03/10</td> --2
            <td class="mcFont txt_r nowrap cls">2020/03/11</td> --3
            <td class="mcFont txt_r nowrap cls">2020/03/13</td> --4
            */

            var trsDividend = trs
                .Where(tr => (tr.Children.Count() == 5 || tr.Children.Count() == 6) && (tr.Children[0].ClassName + "").Contains("cls"));
            var rtv = new List<StockDividend>();
            foreach (var trDiv in trsDividend)
            {
                try
                {
                    var dividend = new StockDividend();
                    //For ETF, only 5 column
                    var varientColIdxCompensate = trDiv.Children.Count() == 5 ? 0 : 1;
                    dividend.DistributionType = StockDividend.DIST_TYPE_CASH_SCRIP; //3 Types: 'Cash/Scrip'/'Cash'/'Scrip'
                    dividend.StockId = stock.GetStockId();
                    dividend.AnnounceDate = DateOnly.Parse(trDiv.Children[0].TextContent);
                    dividend.DividendEvent = dividend.AnnounceDate.ToString("MM");

                    var dividendText = trDiv.Children[1 + varientColIdxCompensate].TextContent;
                    var currency_amt = dividendText.Split(':', ' ', '(');
                    if (currency_amt.Length != 2 ||
                        currency_amt[0].Length != 3 || //Currency should be 3 char
                        !decimal.TryParse(currency_amt[1], out decimal amt)
                        )
                    {
                        continue;
                    }

                    dividend.Amount = amt;
                    dividend.Currency = currency_amt[0];

                    dividend.DividendType = "D";
                    dividend.ExDate = DateOnly.Parse(trDiv.Children[2 + varientColIdxCompensate].TextContent);
                    if (DateOnly.TryParse(trDiv.Children[4 + varientColIdxCompensate].TextContent, out var payableDate))
                    {
                        dividend.PayableDate = payableDate;
                    }
                    rtv.Add(dividend);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Exception when parsing Aastock US div: ");
                    logger.LogWarning(ex.Message);
                    logger.LogWarning(ex.StackTrace);
                }
            }

            return rtv;
        }
    }
}