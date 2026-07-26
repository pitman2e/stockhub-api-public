using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using StockHub.Database;
using StockHub.Services.Position;
using StockHub.Tools;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Services;

[TestSubject(typeof(PositionValueService))]
public class PositionValueServiceTest(ITestOutputHelper testOutputHelper)
{
    private StockHubContext GetSeededContext()
    {
        var context = DbContextMock.Get();
        DatabaseSetup.AddStock(context);
        DatabaseSetup.AddCurrencies(context);
        DatabaseSetup.AddStockPortfolio(context);

        DatabaseSetup.AddStockPrice(context, price: 90, 2022, 1, 3);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 1, 4);
        DatabaseSetup.AddStockPrice(context, price: 80, 2022, 1, 17);
        DatabaseSetup.AddStockPrice(context, price: 80, 2022, 1, 27);
        DatabaseSetup.AddStockPrice(context, price: 79, 2022, 1, 28);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 2, 1);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 2, 2);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 2, 3);
        return context;
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GeneralPositionCalculations(bool isNotUseCachePos)
    {
        var userClaim = UserClaimMock.Get();
        var context = GetSeededContext();

        DatabaseSetup.AddStockTran(context, price: 50, StockTransaction.TRANTYPE_BUY, 2022, 1, 12);

        var div0131 = new StockDividend
        {
            StockId = "99999.HK",
            Currency = "HKD",
            DividendEvent = "FOO",
            DividendType = StockDividend.DIV_TYPE_DIVIDEND,
            DistributionType = StockDividend.DIST_TYPE_CASH,
            AnnounceDate = new DateOnly(2022, 1, 31),
            ExDate = new DateOnly(2022, 1, 28),
            PayableDate = new DateOnly(2022, 1, 31),
            Amount = 1
        };
        context.StockDividends.Add(div0131);

        var div0131Tran = new StockTransaction
        {
            Uid = userClaim.GetUid(),
            PortfolioId = "TEST_P",
            StockId = div0131.StockId,
            TxDate = div0131.PayableDate,
            Currency = div0131.Currency,
            TxCount = div0131.Amount.GetValueOrDefault(),
            UnitAmt = 1,
            TranType = StockTransaction.TRANTYPE_DIV
        };
        context.StockTransactions.Add(div0131Tran);

        var exReinvDate = new DateOnly(2022, 2, 2);

        {
            var div = new StockDividend
            {
                StockId = "99999.HK",
                Currency = "HKD",
                DividendEvent = "FOO",
                DividendType = StockDividend.DIV_TYPE_DIVIDEND,
                DistributionType = StockDividend.DIST_TYPE_CASH,
                AnnounceDate = exReinvDate,
                ExDate = exReinvDate,
                PayableDate = exReinvDate,
                Amount = 1
            };
            context.StockDividends.Add(div);

            var realisedScripTran = new StockTransaction
            {
                Uid = userClaim.GetUid(),
                PortfolioId = "TEST_P",
                StockId = div.StockId,
                TxDate = div.PayableDate,
                Currency = div.Currency,
                TxCount = 0.0125m,
                UnitAmt = 80,
                TranType = StockTransaction.TRANTYPE_REINV,
                HandlingFee = -0.1m,
                Tax = -0.5m
            };
            context.StockTransactions.Add(realisedScripTran);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var posValServ = new PositionValueService(context);
        var upsFilter = UPSFilter.GetFilter(userClaim.GetUid(), portfolioId: "TEST_P");

        var dateFmDate = new DateOnly(2022, 1, 28);
        var dateToDate = new DateOnly(2022, 2, 3);

        var stockPosVals = await posValServ.GetStockPositionValuesAsync(upsFilter: upsFilter,
            dateFmDate: dateFmDate,
            dateToDate: dateToDate,
            isSkipNonmarketDate: false,
            positionStatus: PositionValueService.PositionStatus.Any,
            isNotUseCachePos: isNotUseCachePos);

        var stockPosVal = stockPosVals.Last();
        Assert.Equal(dateToDate, stockPosVal.MarketDate);
        Assert.Equal(0, stockPosVal.RealisedCost);
        Assert.Equal(1 + (0.0125m), stockPosVal.Quantity);
        Assert.Equal(50 + (0.0125m) * 80 + 0.1m + 0.5m, stockPosVal.UnrealisedCost);
        Assert.Equal(10 + (0.0125m) * 10, stockPosVal.UnrealisedAmount);
        Assert.Equal((10 + (0.0125m) * 10) - ((50 + (0.0125m) * 80 + 0.1m + 0.5m)), stockPosVal.UnrealisedGain);
        Assert.Equal(1 + ((0.0125m) * 80), stockPosVal.RealisedDividend);
        Assert.Equal(50 + ((0.0125m) * 80) + 0.1m + 0.5m, stockPosVal.TotalCost);
    }

    /// <summary>
    /// Position Current Gain exclude loss due to the ticker in ex-dividend date, defined in Dividend Table
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CurrentGainOffsetByDivEx(bool isNotUseCachePos)
    {
        var userClaim = UserClaimMock.Get();
        var context = GetSeededContext();
        DatabaseSetup.AddStockTran(context, price: 50, StockTransaction.TRANTYPE_BUY, 2022, 1, 12);

        var div = new StockDividend
        {
            StockId = "99999.HK",
            Currency = "HKD",
            DividendEvent = "FOO",
            DividendType = StockDividend.DIV_TYPE_DIVIDEND,
            DistributionType = StockDividend.DIST_TYPE_CASH,
            AnnounceDate = new DateOnly(2022, 1, 28),
            ExDate = new DateOnly(2022, 1, 28),
            PayableDate = new DateOnly(2022, 1, 31),
            Amount = 1
        };
        context.StockDividends.Add(div);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var posValServ = new PositionValueService(context);
        var upsFilter = UPSFilter.GetFilter(userClaim.GetUid(), portfolioId: "TEST_P");

        var stockPosVals = await posValServ.GetStockPositionValuesAsync(upsFilter: upsFilter,
            dateFmDate: div.ExDate,
            dateToDate: div.ExDate,
            isSkipNonmarketDate: false,
            positionStatus: PositionValueService.PositionStatus.Any,
            isNotUseCachePos: isNotUseCachePos);

        var exDatePosVal = stockPosVals.First(p => p.MarketDate == div.ExDate);
        Assert.Equal(0, exDatePosVal.CurrentGain); //At Ex Date, Daily Gain should not include lose from Div Ex
    }
    
    /// <summary>
    /// Use the latest Price from table or the latest transaction price to calculate unrealized amount
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UseLatestTranPriceAsPrice(bool isNotUseCachePos)
    {
        var userClaim = UserClaimMock.Get();
        var context = GetSeededContext();
        DatabaseSetup.AddStockTran(context, price: 10, StockTransaction.TRANTYPE_BUY, 2021, 1, 1);
        var tranBuy0101 = 
            DatabaseSetup.AddStockTran(context, price: 200, StockTransaction.TRANTYPE_BUY, 2026, 1, 1);
        var tranSell0102 = 
            DatabaseSetup.AddStockTran(context, price: 300, StockTransaction.TRANTYPE_SELL, 2026, 1, 2);

        var posValServ = new PositionValueService(context);
        var upsFilter = UPSFilter.GetFilter(userClaim.GetUid(), portfolioId: "TEST_P");

        var stockPosVals = await posValServ.GetStockPositionValuesAsync(upsFilter: upsFilter,
            dateFmDate: new DateOnly(2026, 1, 1),
            dateToDate: new DateOnly(2026, 1, 2),
            isSkipNonmarketDate: false,
            positionStatus: PositionValueService.PositionStatus.Any,
            isNotUseCachePos: isNotUseCachePos);

        var posValBuy0101 = stockPosVals.First(x => x.ObserveDate == tranBuy0101.TxDate);
        Assert.Equal(tranBuy0101.TxDate, posValBuy0101.MarketDate);
        Assert.Equal(tranBuy0101.UnitAmt, posValBuy0101.StockPrice);
        Assert.Equal(2 * tranBuy0101.UnitAmt, posValBuy0101.UnrealisedAmount);
        
        var posValSell0102 = stockPosVals.First(x => x.ObserveDate == tranSell0102.TxDate);
        Assert.Equal(tranSell0102.TxDate, posValSell0102.MarketDate);
        Assert.Equal(tranSell0102.UnitAmt, posValSell0102.StockPrice);
        Assert.Equal(1 * tranSell0102.UnitAmt, posValSell0102.UnrealisedAmount);
    }
    
    /// <summary>
    /// Do not use the latest Price from transaction price if its TranType is CASH or REINV or DIV
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NotUseLatestCashOrDivTranPriceAsPrice(bool isNotUseCachePos)
    {
        var userClaim = UserClaimMock.Get();
        var context = GetSeededContext();
        DatabaseSetup.AddStockTran(context, price: 200, StockTransaction.TRANTYPE_CASH, 2026, 1, 1);
        DatabaseSetup.AddStockTran(context, price: 300, StockTransaction.TRANTYPE_REINV, 2026, 1, 2);
        DatabaseSetup.AddStockTran(context, price: 400, StockTransaction.TRANTYPE_DIV, 2026, 1, 5);

        var posValServ = new PositionValueService(context);
        var upsFilter = UPSFilter.GetFilter(userClaim.GetUid(), portfolioId: "TEST_P");

        var stockPosVals = await posValServ.GetStockPositionValuesAsync(upsFilter: upsFilter,
            dateFmDate: new DateOnly(2026, 1, 1),
            dateToDate: new DateOnly(2026, 1, 5),
            isSkipNonmarketDate: false,
            positionStatus: PositionValueService.PositionStatus.Any,
            isNotUseCachePos: isNotUseCachePos);

        var posValCash = stockPosVals.First(x => x.ObserveDate == new DateOnly(2026, 1, 1));
        Assert.NotEqual(200, posValCash.StockPrice);
        
        var posValReinv = stockPosVals.First(x => x.ObserveDate == new DateOnly(2026, 1, 2));
        Assert.NotEqual(300, posValReinv.StockPrice);
        
        var posValDiv = stockPosVals.First(x => x.ObserveDate == new DateOnly(2026, 1, 5));
        Assert.NotEqual(400, posValDiv.StockPrice);
    }
    
    /// <summary>
    /// When a stock is sold, it should still count in current gain but only at the price you sold it
    /// </summary>
    [Theory(Skip = "Not implemented yet")] // TODO
    [InlineData(true)]
    [InlineData(false)]
    public async Task CurrentGainSoldToday(bool isNotUseCachePos)
    {
        var userClaim = UserClaimMock.Get();
        var context = GetSeededContext();
        DatabaseSetup.AddStockPrice(context, price: 200, 2026, 1, 1);
        DatabaseSetup.AddStockTran(context, price: 200, StockTransaction.TRANTYPE_BUY, 2026, 1, 1);
        DatabaseSetup.AddStockTran(context, price: 300, StockTransaction.TRANTYPE_SELL, 2026, 1, 2);
        DatabaseSetup.AddStockPrice(context, price: 400, 2026, 1, 2);

        var posValServ = new PositionValueService(context);
        var upsFilter = UPSFilter.GetFilter(userClaim.GetUid(), portfolioId: "TEST_P");

        var stockPosVals = await posValServ.GetStockPositionValuesAsync(upsFilter: upsFilter,
            dateFmDate: new DateOnly(2026, 1, 2),
            dateToDate: new DateOnly(2026, 1, 2),
            isSkipNonmarketDate: false,
            positionStatus: PositionValueService.PositionStatus.Any,
            isNotUseCachePos: isNotUseCachePos);

        var pos = stockPosVals.First(x => x.ObserveDate == new DateOnly(2026, 1, 2));
        Assert.Equal(100, pos.CurrentGain);
        Assert.Equal(100, pos.TotalGain);
    }

    [Fact]
    public async Task Bonds()
    {
        var userClaim = UserClaimMock.Get();
        var context = DbContextMock.Get();
            
        StockPortfolio portfolio = new StockPortfolio
        {
            Uid = userClaim.GetUid(),
            PortfolioId = "TEST_P",
            Name = "TEST_P_NAME",
            DefaultCurrency = "USD",
            Priority = 1
        };
        context.StockPortfolios.Add(portfolio);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        DatabaseSetup.AddBond(context);

        StockTransaction tran = new StockTransaction
        {
            Uid = userClaim.GetUid(),
            PortfolioId = "TEST_P",
            StockId = "USBOND.USBND",
            TxCount = 10,
            Currency = "USD",
            TranType = StockTransaction.TRANTYPE_BUY,
            UnitAmt = 100,
            AccruedInterest = -6,
            HandlingFee = -7,
            TxDate = new DateOnly(2024, 6, 18)
        };
        context.StockTransactions.Add(tran);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var posValServ = new PositionValueService(context);
        var upsFilter = UPSFilter.GetFilter(userClaim.GetUid(), portfolioId: "TEST_P");

        var dateFmDate = new DateOnly(2024, 6, 18);
        var dateToDate = new DateOnly(2024, 6, 18);

        var stockPosVals = await posValServ.GetStockPositionValuesAsync(upsFilter: upsFilter,
            dateFmDate: dateFmDate,
            dateToDate: dateToDate,
            isSkipNonmarketDate: false,
            positionStatus: PositionValueService.PositionStatus.Any);

        var stockPosVal = stockPosVals.Last();
        //Assert.Equal(dateToDate, stockPosVal.MarketDate);
        Assert.Equal(0, stockPosVal.RealisedCost);
        Assert.Equal(10, stockPosVal.Quantity);
        Assert.Equal(100 * 10 + 7, stockPosVal.UnrealisedCost);
        Assert.Equal(100 * 10, stockPosVal.UnrealisedAmount);
        Assert.Equal(-7, stockPosVal.UnrealisedGain);
        Assert.Equal(-6, stockPosVal.RealisedDividend);
        Assert.Equal(0, stockPosVal.RealisedCost);
        Assert.Equal(100 * 10 + 7, stockPosVal.TotalCost);
    }
}