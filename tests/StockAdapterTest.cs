using System;
using Moq;
using StockHub.Crawlers.Price;
using StockHub.Exchanges;
using StockHub.Exchanges.ConcreteExchanges;
using Xunit;
using StockHub.Services;

namespace UnitTests;

public class StockAdapterTest
{
    private readonly AllExchanges _allExchanges;
    
    public StockAdapterTest()
    {
        var yfinanceMock = new Mock<IYfinancePriceCrawler>();
        var hsbcMock = new Mock<IHsbcPriceCrawler>();

        _allExchanges = new AllExchanges(
            us: new US(yfinanceMock.Object),
            lse: new LSE(yfinanceMock.Object),
            hk: new HK(yfinanceMock.Object),
            hsbc: new HSBC(hsbcMock.Object),
            pcp: new PCP(),
            manu: new MANU(),
            cash: new CASH(),
            usbnd: new USBND(),
            hkbnd: new HKBND()
        );
    }
    
    [Theory]
    [InlineData("00001.HK", HK.MARKET_ID, "0001.HK", "00001", "")]
    [InlineData("ABC.US", US.MARKET_ID, "ABC", "ABC", "ABC")]
    [InlineData("HSBCP.HSBC", HSBC.MARKET_ID)]
    [InlineData("SHK123.MANU", MANU.MARKET_ID)]
    [InlineData("CORE.PCP", PCP.MARKET_ID)]
    [InlineData("FIHKD.CASH", CASH.MARKET_ID)]
    [InlineData("912800000.USBND", USBND.MARKET_ID)]
    [InlineData("00GB0000R.HKBND", HKBND.MARKET_ID)]
    [InlineData("LSLE.LSE", LSE.MARKET_ID, "LSLE.L")]
    public void ParseExact(string stockId,
        string expectExchangeMarket,
        string expectYahooStockId = "",
        string expectAastockStockId = "",
        string expectNasdaqStockId = "")
    {
        var stock2ExchangeService = new Stock2ExchangeService(_allExchanges);
        var stock = stock2ExchangeService.ParseExact(stockId);
        Assert.Equal(expectExchangeMarket, stock.Exchange.MarketId);

        if (!string.IsNullOrWhiteSpace(expectAastockStockId))
        {
            Assert.Equal(expectAastockStockId, stock.ToAastockStockId());
        }
        else
        {
            Assert.ThrowsAny<Exception>(stock.ToAastockStockId);
        }
            
        if (!string.IsNullOrWhiteSpace(expectYahooStockId))
        {
            Assert.Equal(expectYahooStockId, stock.ToYahooStockId());
        }
        else
        {
            Assert.ThrowsAny<Exception>(stock.ToYahooStockId);
        }
            
        if (!string.IsNullOrWhiteSpace(expectNasdaqStockId))
        {
            Assert.Equal(expectYahooStockId, stock.ToNasdaqStockId());
        }
        else
        {
            Assert.ThrowsAny<Exception>(stock.ToNasdaqStockId);
        }
    }
}