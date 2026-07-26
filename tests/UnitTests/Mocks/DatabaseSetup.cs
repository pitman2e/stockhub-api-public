using System;
using System.Linq;
using StockHub.Database;

namespace UnitTests.Mocks;

public abstract class DatabaseSetup
{
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
    
    public static void AddStock(StockHubContext context, string stockId, string currency)
    {
        var stock = new Stock
        {
            Currency = currency,
            StockId = stockId,
            StockName = stockId,
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
        UserClaimMock userClaimMock = new UserClaimMock();
        portfolio.Uid = userClaimMock.GetUid();
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
        Throws_IfNotWeekday(marketDate);

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

    public static StockTransaction AddStockTran(StockHubContext context, decimal price, string tranType, int year, int month, int day)
    {
        return AddStockTran(context, price, tranType, new DateOnly(year, month, day));
    }
    
    public static StockTransaction AddStockTran(StockHubContext context, decimal price, string tranType, DateOnly dt)
    {
        Throws_IfNotWeekday(dt);
        
        UserClaimMock userClaimMock = new UserClaimMock();
        StockTransaction tran = new StockTransaction
        {
            Uid = userClaimMock.GetUid(),
            PortfolioId = "TEST_P",
            StockId = "99999.HK",
            TxCount = tranType == StockTransaction.TRANTYPE_SELL ? -1 : 1,
            Currency = "HKD",
            TranType = tranType,
            UnitAmt = price,
            TxDate = dt
        };
        context.StockTransactions.Add(tran);
        context.SaveChanges();
        return tran;
    }

    public static void AddCurrencies(StockHubContext context)
    {
        context.Currencies.Add(new Currency
        {
            CurrencyId = "HKD",
            CurrencyName = "Hong Kong Dollars",
            ToUsdRate = 7.8m
        });
        
        context.Currencies.Add(new Currency
        {
            CurrencyId = "USD",
            CurrencyName = "US Dollars",
            ToUsdRate = 1m
        });
        
        context.SaveChanges();
    }

    private static void Throws_IfNotWeekday(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Sunday or DayOfWeek.Saturday)
        {
            throw new ArgumentException("Date should be a weekday");
        }
    }
}