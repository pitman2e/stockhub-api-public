using Moq;
using StockHub.Crawlers.Price;
using StockHub.Exchanges;
using StockHub.Exchanges.ConcreteExchanges;

namespace UnitTests.Mocks;

public static class AllExchangesMock
{
    public static AllExchanges Get()
    {
        var yfinanceMock = new Mock<IYfinancePriceCrawler>();
        var hsbcMock = new Mock<IHsbcPriceCrawler>();

        return new AllExchanges([
            new US(yfinanceMock.Object),
            new LSE(yfinanceMock.Object),
            new HK(yfinanceMock.Object),
            new HSBC(hsbcMock.Object),
            new PCP(),
            new MANU(),
            new CASH(),
            new USBND(),
            new HKBND()
        ]);
    }
}