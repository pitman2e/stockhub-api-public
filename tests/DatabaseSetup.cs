using System;
using System.Linq;
using StockHub.Database;

namespace UnitTests;

public class DatabaseSetup
{
    public static void Cleanup(StockHubContext context)
    {
        context.Stocks.RemoveRange(context.Stocks);
        context.StockPrices.RemoveRange(context.StockPrices);
        context.StockPortfolios.RemoveRange(context.StockPortfolios);
        context.StockTransactions.RemoveRange(context.StockTransactions);
        context.StockDividends.RemoveRange(context.StockDividends);
        context.StockRealisedScrips.RemoveRange(context.StockRealisedScrips);
        //var allCurrencies = context.Currencies.Where(s => s.CurrenyId == "TSD");
        //context.Currencies.RemoveRange(allCurrencies);
        context.SaveChanges();
    }

    public static void AddStock(StockHubContext context)
    {
        var stock = new Stock
        {
            Currency = "HKD",
            StockId = "99999.HK",
            StockName = "TEST NAME",
            AssetClass = "STOCK",
            Coupon = null,
            CouponFreq = null,
            MaturityDate = null,
            FaceValue = null
        };
        context.Stocks.Add(stock);
        context.SaveChanges();
    }

    public static void AddBond(StockHubContext context)
    {
        var stock = new Stock
        {
            Currency = "USD",
            StockId = "USBOND.USBND",
            StockName = "US BOND NAME",
            AssetClass = Stock.ASSET_CLASS_BOND,
            Coupon = null,
            CouponFreq = null,
            MaturityDate = null,
            FaceValue = null
        };
        context.Stocks.Add(stock);
        context.SaveChanges();
    }

    public static void AddStockPortfolio(StockHubContext context)
    {
        StockPortfolio portfolio = new StockPortfolio();
        TestUserClaim userClaim = new TestUserClaim();
        portfolio.Uid = userClaim.GetUid();
        portfolio.PortfolioId = "TEST_P";
        portfolio.Name = "TEST_P_NAME";
        portfolio.DefaultCurrency = "HKD";
        portfolio.Priority = 1;
        context.StockPortfolios.Add(portfolio);
        context.SaveChanges();
    }

    public static void AddStockPrice(StockHubContext context, decimal price, int year, int month, int day)
    {
        var marketDate = new DateOnly(year, month, day);
        if (marketDate.DayOfWeek == DayOfWeek.Sunday || marketDate.DayOfWeek == DayOfWeek.Saturday)
        {
            throw new ArgumentException("Date should be a weekday");
        }

        StockPrice stockprice = new StockPrice
        {
            StockId = "99999.HK",
            MarketDate = marketDate,
            OpenPrice = price,
            ClosePrice = price
        };
        context.StockPrices.Add(stockprice);
        context.SaveChanges();
    }

    public static void AddStockTran(StockHubContext context, decimal price, string tranType, int year, int month, int day)
    {
        TestUserClaim userClaim = new TestUserClaim();
        StockTransaction tran = new StockTransaction
        {
            Uid = userClaim.GetUid(),
            PortfolioId = "TEST_P",
            StockId = "99999.HK",
            TxCount = 1,
            Currency = "HKD",
            TranType = tranType,
            UnitAmt = price,
            TxDate = new DateOnly(year, month, day)
        };
        context.StockTransactions.Add(tran);
        context.SaveChanges();
    }

    public static void AddCurrency(StockHubContext context)
    {
        Currency currency = new Currency
        {
            CurrencyId = "HKD",
            CurrencyName = "Hong Kong Dollars",
            ToUsdRate = 7.8m
        };

        if (!context.Currencies.Any(c => c.CurrencyId == currency.CurrencyId))
        {
            context.Currencies.Add(currency);
        }
        context.SaveChanges();
    }

    public static void AddUSDCurrency(StockHubContext context)
    {
        Currency currency = new Currency
        {
            CurrencyId = "USD",
            CurrencyName = "US Dollars",
            ToUsdRate = 1m
        };

        if (!context.Currencies.Any(c => c.CurrencyId == currency.CurrencyId))
        {
            context.Currencies.Add(currency);
        }
        context.SaveChanges();
    }
}