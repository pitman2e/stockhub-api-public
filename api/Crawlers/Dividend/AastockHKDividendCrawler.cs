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

public class AastockHKDividendCrawler(
    HttpClient httpClient,
    ILogger<AastockHKDividendCrawler> logger)
    : IAastockHKDividendCrawler
{
    public async Task<IEnumerable<StockDividend>> CrawlAsync(StockAdapter stock)
    {
        var config = Configuration.Default.WithDefaultLoader();
        var browsingContext = BrowsingContext.New(config);
        var aastockId = AastocksStockIdMapper.Map(stock);
        var document = await browsingContext.OpenAsync($"http://www.aa" +
                                                       $"sto" +
                                                       $"cks.com/en/stocks/analysis/dividend.aspx?symbol={aastockId}");

        {
            var selector = "table.cnhk-cf>tbody>tr";
            var trs = document.QuerySelectorAll(selector);

            /*
            <td class="mcFont nowrap cls">2019/08/01</td>   --0
            <td class="mcFont txt_r nowrap cls">2019/12</td> --1
            <td class="mcFont txt_l cls" style="padding-left:10px">Interim</td> --2
            <td class="mcFont txt_l cls"><a href="javascript:GoToDH('D')" class="a15">D</a>:HKD
                0.8700</td> --3
            <td class="mcFont txt_r nowrap cls">Cash</td> --4
            <td class="mcFont txt_r nowrap cls">2019/09/02</td> --5
            <td class="mcFont txt_r nowrap cls">2019/09/03</td> --6
            <td class="mcFont txt_r nowrap cls">2019/09/12</td> --7
            */

            var trsDividend = trs
                .Where(tr => tr.Children.Count() == 8 && (tr.Children[0].ClassName + "").Contains("cls"));
            var rtv = new List<StockDividend>();
            foreach (var trDiv in trsDividend)
            {
                try
                {
                    var dividend = new StockDividend
                    {
                        DistributionType = trDiv.Children[4].TextContent, //3 Types: 'Cash/Scrip'/'Cash'/'Scrip'
                        StockId = stock.GetStockId(),
                        AnnounceDate = DateOnly.Parse(trDiv.Children[0].TextContent),
                        DividendEvent = trDiv.Children[2].TextContent
                    };
                    if (dividend.DistributionType == StockDividend.DIST_TYPE_SCRIP)
                    {
                        var scripTexts = trDiv.Children[3].TextContent.Trim().Split("-"); //Expect this text: B:1-for-10
                        if (scripTexts.Length != 3 ||
                            scripTexts[0] != "B:1" ||
                            scripTexts[1] != "for" ||
                            !decimal.TryParse(scripTexts[2], out decimal scripPerCount))
                        {
                            continue;
                        }

                        dividend.ScripPerCount = scripPerCount;
                    }
                    else
                    {
                        var dividendText = trDiv.Children[3].TextContent;
                        var currencyAmt = dividendText.Split(':', ' ', '(');
                        if (currencyAmt.Length < 3 ||
                            currencyAmt[1].Length != 3 || //Currency should be 3 char
                            currencyAmt[1].Any(c => c >= '0' && c <= '9') || //Should have no number
                            !decimal.TryParse(currencyAmt[2], out decimal amt)
                            )
                        {
                            continue;
                        }

                        dividend.Amount = amt;
                        dividend.Currency = currencyAmt[1];
                    }

                    dividend.DividendType = trDiv.Children[3].FirstChild.TextContent;
                    if (dividend.DistributionType == "-")
                    {
                        logger.LogWarning("Skip Div entry because Distribution Type is '-'");
                        continue;
                    }
                    dividend.ExDate = DateOnly.Parse(trDiv.Children[5].TextContent);
                    if (DateOnly.TryParse(trDiv.Children[7].TextContent, out var payableDate))
                    {
                        dividend.PayableDate = payableDate;
                    }
                    rtv.Add(dividend);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("Exception when parsing Aastock HK div: ");
                    logger.LogWarning(ex.Message);
                    logger.LogWarning(ex.StackTrace);
                }
            }

            return rtv;
        }
    }
}