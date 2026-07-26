using JetBrains.Annotations;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Services;

[TestSubject(typeof(Stock2ExchangeService))]
public class Stock2ExchangeServiceTest
{
    [Theory]
    [InlineData("00001.HK", HK.MARKET_ID)]
    [InlineData("ABC.US", US.MARKET_ID)]
    [InlineData("HSBCP.HSBC", HSBC.MARKET_ID)]
    [InlineData("SHK123.MANU", MANU.MARKET_ID)]
    [InlineData("CORE.PCP", PCP.MARKET_ID)]
    [InlineData("FIHKD.CASH", CASH.MARKET_ID)]
    [InlineData("912800000.USBND", USBND.MARKET_ID)]
    [InlineData("00GB0000R.HKBND", HKBND.MARKET_ID)]
    [InlineData("LSLE.LSE", LSE.MARKET_ID)]
    public void ParseExact(string stockId,
        string expectExchangeMarket)
    {
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        var stock = stock2ExchangeService.ParseExact(stockId);
        Assert.Equal(expectExchangeMarket, stock.Exchange.MarketId);
    }
}