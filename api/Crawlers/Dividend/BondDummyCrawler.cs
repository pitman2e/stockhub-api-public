using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockHub.Database;
using StockHub.Exchanges;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Models;
using StockHub.Services;

namespace StockHub.Crawlers.Dividend;

public class BondDummyCrawler(
    StockHubContext context,
    ILogger<BondDummyCrawler> logger)
    : IBondDummyCrawler
{
    public async Task<IEnumerable<StockDividend>> CrawlAsync(StockAdapter inStock)
    {
        var nowDateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
        var rtv = new List<StockDividend>();
        
        // TODO: Use Stock then Include Pos?
        var bonds = await
            (
                from sp in context.StockPositions.Include(sp => sp.FkStock)
                where sp.StockId == inStock.GetStockId()
                where sp.IsLatest
                where sp.StockId.EndsWith(HKBND.MARKET_ID) || sp.StockId.EndsWith(USBND.MARKET_ID)
                where sp.FkStock.AssetClass == Stock.ASSET_CLASS_BOND
                where sp.FkStock.Coupon.GetValueOrDefault() > 0
                where sp.FkStock.CouponFreq.GetValueOrDefault() > 0
                where sp.FkStock.MaturityDate != null
                select sp.FkStock
            )
            .Distinct()
            .Where(sp => sp.MaturityDate != null && sp.MaturityDate >= nowDateOnly)
            .ToListAsync();
        
        foreach (var bond in bonds)
        {
            if (!new []{1, 2, 3, 4, 6, 12}.Contains(bond.CouponFreq.GetValueOrDefault()))
            {
                logger.LogInformation($"Bond Coupon Freq {bond.CouponFreq} is not supported for {bond.StockId}");
                continue;
            }
            
            int monthGap = 12 / bond.CouponFreq.GetValueOrDefault();
            if (bond.MaturityDate.GetValueOrDefault().Month % monthGap == nowDateOnly.Month % monthGap)
            {
                var dividend = new StockDividend
                {
                    StockId = bond.StockId,
                    AnnounceDate = new DateOnly(nowDateOnly.Year, nowDateOnly.Month, bond.MaturityDate.GetValueOrDefault().Day)
                };
                dividend.DividendEvent = dividend.AnnounceDate.ToString("MM");
                dividend.DistributionType = StockDividend.DIST_TYPE_CASH; //3 Types: 'Cash/Scrip'/'Cash'/'Scrip'
                dividend.Amount = (bond.FaceValue ?? 100m) * bond.Coupon / 100 / bond.CouponFreq;
                dividend.Currency = bond.Currency;
                dividend.DividendType = StockDividend.DIV_TYPE_DIVIDEND;
                dividend.ExDate = dividend.AnnounceDate;
                dividend.PayableDate = dividend.AnnounceDate;
                rtv.Add(dividend);
            }
        }
        
        return rtv;
    }
}