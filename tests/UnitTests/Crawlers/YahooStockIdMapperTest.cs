using JetBrains.Annotations;
using StockHub.Crawlers;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Crawlers;

[TestSubject(typeof(YahooStockIdMapper))]
public class YahooStockIdMapperTest
{
    [Theory]
    [InlineData("00001.HK", "0001.HK")]
    [InlineData("ABC.US", "ABC")]
    [InlineData("CORE.PCP", null)]
    [InlineData("LSLE.LSE", "LSLE.L")]
    public void Map(string stockId,
        string? expectYahooStockId)
    {
        //TODO: Remove deps to StockAdapter
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        Assert.Equal(expectYahooStockId, YahooStockIdMapper.Map(stock2ExchangeService.ParseExact(stockId)));
    }
}