using JetBrains.Annotations;
using StockHub.Crawlers;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Crawlers;

[TestSubject(typeof(AastocksStockIdMapper))]
public class AastocksStockIdMapperTest
{
    [Theory]
    [InlineData("00001.HK", "00001")]
    [InlineData("ABC.US", "ABC")]
    public void Map(string stockId,
        string? expectedStockId)
    {
        //TODO: Remove deps to StockAdapter
        var stock2ExchangeService = new Stock2ExchangeService(AllExchangesMock.Get());
        Assert.Equal(expectedStockId, AastocksStockIdMapper.Map(stock2ExchangeService.ParseExact(stockId)));
    }
}