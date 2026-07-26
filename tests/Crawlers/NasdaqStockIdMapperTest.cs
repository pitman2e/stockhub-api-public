using JetBrains.Annotations;
using StockHub.Crawlers;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Crawlers;

[TestSubject(typeof(NasdaqStockIdMapper))]
public class NasdaqStockIdMapperTest
{
    [Theory]
    [InlineData("00001.HK", null)]
    [InlineData("ABC.US", "ABC")]
    public void Map(string stockId,
        string? expectedStockId)
    {
        //TODO: Remove deps to StockAdapter
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        Assert.Equal(expectedStockId, NasdaqStockIdMapper.Map(stock2ExchangeService.ParseExact(stockId)));
    }
}