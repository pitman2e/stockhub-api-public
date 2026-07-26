using System;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using StockHub.Services;
using StockHub.Database;
using UnitTests.Constants;
using StockHub.Services.Position;
using StockHub.Tools;

namespace UnitTests;

[Collection(TestCollection.DATABASE)]
public class PositionValueServiceTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public async Task Stocks()
    {
        var context = TestStockHubContext.Get();
        Assert.SkipWhen(context == null, "Db context is null");

        var userClaim = TestUserClaim.Get();

        DatabaseSetup.Cleanup(context);
        DatabaseSetup.AddStock(context);
        DatabaseSetup.AddCurrency(context);
        DatabaseSetup.AddStockPortfolio(context);

        DatabaseSetup.AddStockPrice(context, price: 90, 2022, 1, 3);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 1, 4);
        DatabaseSetup.AddStockPrice(context, price: 80, 2022, 1, 17);
        DatabaseSetup.AddStockPrice(context, price: 80, 2022, 1, 27);
        DatabaseSetup.AddStockPrice(context, price: 79, 2022, 1, 28);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 2, 1);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 2, 2);
        DatabaseSetup.AddStockPrice(context, price: 10, 2022, 2, 3);

        DatabaseSetup.AddStockTran(context, price: 50, StockTransaction.TRANTYPE_BUY, 2022, 1, 12);

        var exDate = new DateOnly(2022, 1, 28);

        {
            var div = new StockDividend
            {
                StockId = "99999.HK",
                Currency = "HKD",
                DividendEvent = "FOO",
                DividendType = StockDividend.DIV_TYPE_DIVIDEND,
                DistributionType = StockDividend.DIST_TYPE_CASH,
                AnnounceDate = new DateOnly(2022, 1, 31),
                ExDate = exDate,
                PayableDate = new DateOnly(2022, 1, 31),
                Amount = 1
            };
            context.StockDividends.Add(div);

            var divTran = new StockTransaction
            {
                Uid = userClaim.GetUid(),
                PortfolioId = "TEST_P",
                StockId = div.StockId,
                TxDate = div.PayableDate,
                Currency = div.Currency,
                TxCount = div.Amount.GetValueOrDefault(),
                UnitAmt = 1,
                TranType = StockTransaction.TRANTYPE_DIV
            };
            context.StockTransactions.Add(divTran);
        }

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

            //This entry has no actual usage, it is a reference to generate the transaction
            var realisedScrip = new StockRealisedScrip
            {
                Uid = userClaim.GetUid(),
                PortfolioId = "TEST_P",
                StockId = div.StockId,
                DistributionType = div.DistributionType,
                DividendType = div.DividendType,
                PayableDate = div.PayableDate,
                ReinvestPrice = 80,
                ScripReceived = 0.0125m // 1/80
            };
            context.StockRealisedScrips.Add(realisedScrip);

            var realisedScripTran = new StockTransaction
            {
                Uid = userClaim.GetUid(),
                PortfolioId = realisedScrip.PortfolioId,
                StockId = realisedScrip.StockId,
                TxDate = realisedScrip.PayableDate,
                Currency = div.Currency,
                TxCount = realisedScrip.ScripReceived,
                UnitAmt = realisedScrip.ReinvestPrice.GetValueOrDefault(),
                TranType = StockTransaction.TRANTYPE_REINV
            };
            context.StockTransactions.Add(realisedScripTran);
        }

        {
            var exReinvDateNotExact = new DateOnly(2022, 2, 3);

            var div = new StockDividend
            {
                StockId = "99999.HK",
                Currency = "HKD",
                DividendEvent = "FOO",
                DividendType = StockDividend.DIV_TYPE_DIVIDEND,
                DistributionType = StockDividend.DIST_TYPE_CASH,
                AnnounceDate = exReinvDateNotExact,
                ExDate = exReinvDateNotExact,
                PayableDate = exReinvDateNotExact,
                Amount = 0.8m
            };
            context.StockDividends.Add(div);

            //This entry has no actual usage, it is a reference to generate the transaction
            var realisedScrip = new StockRealisedScrip
            {
                Uid = userClaim.GetUid(),
                PortfolioId = "TEST_P",
                StockId = div.StockId,
                DistributionType = div.DistributionType,
                DividendType = div.DividendType,
                PayableDate = div.PayableDate,
                ReinvestPrice = 80,
                ScripReceived = 0.01m // Not exact amount due to tax
            };
            context.StockRealisedScrips.Add(realisedScrip);

            var realisedScripTran = new StockTransaction
            {
                Uid = userClaim.GetUid(),
                PortfolioId = realisedScrip.PortfolioId,
                StockId = realisedScrip.StockId,
                TxDate = realisedScrip.PayableDate,
                Currency = div.Currency,
                TxCount = realisedScrip.ScripReceived,
                UnitAmt = realisedScrip.ReinvestPrice.GetValueOrDefault(),
                TranType = StockTransaction.TRANTYPE_REINV,
                HandlingFee = -0.1m,
                Tax = -0.5m
            };
            context.StockTransactions.Add(realisedScripTran);
        }

        context.SaveChanges();

        var posValServ = new PositionValueService(context);
        var upsFilter = UPSFilter.GetFilter(userClaim.GetUid(), portfolioId: "TEST_P");

        var dateFmDate = new DateOnly(2022, 1, 28);
        var dateToDate = new DateOnly(2022, 2, 3);

        var stockPosVals = await posValServ.GetStockPositionValuesAsync(upsFilter: upsFilter,
            dateFmDate: dateFmDate,
            dateToDate: dateToDate,
            isSkipNonmarketDate: false,
            positionStatus: PositionValueService.PositionStatus.Any);

        var exDatePosVal = stockPosVals.First(p => p.MarketDate == exDate);
        Assert.Equal(0, exDatePosVal.CurrentGain); //At Ex Date, Daily Gain should not include lose from Div Ex

        var exReinvDatePosVal = stockPosVals.First(p => p.MarketDate == exReinvDate);
        Assert.Equal(0, exDatePosVal.CurrentGain); //At Ex Rev Date, Daily Gain should not include lose from Div Ex

        var stockPosVal = stockPosVals.Last();
        Assert.Equal(dateToDate, stockPosVal.MarketDate);
        Assert.Equal(0, stockPosVal.RealisedCost);
        Assert.Equal(1 + (0.0125m + 0.01m), stockPosVal.Quantity);
        Assert.Equal(50 + (0.0125m + 0.01m) * 80 + 0.1m + 0.5m, stockPosVal.UnrealisedCost);
        Assert.Equal(10 + (0.0125m + 0.01m) * 10, stockPosVal.UnrealisedAmount);
        Assert.Equal((10 + (0.0125m + 0.01m) * 10) - ((50 + (0.0125m + 0.01m) * 80 + 0.1m + 0.5m)), stockPosVal.UnrealisedGain);
        Assert.Equal(1 + ((0.0125m + 0.01m) * 80), stockPosVal.RealisedDividend);
        Assert.Equal(50 + ((0.0125m + 0.01m) * 80) + 0.1m + 0.5m, stockPosVal.TotalCost);
    }

    [Fact]
    public async Task Bonds()
    {
        var context = TestStockHubContext.Get();
        Assert.SkipWhen(context == null, "Db context is null");

        var userClaim = TestUserClaim.Get();

        DatabaseSetup.Cleanup(context);
        DatabaseSetup.AddUSDCurrency(context);

        StockPortfolio portfolio = new StockPortfolio
        {
            Uid = userClaim.GetUid(),
            PortfolioId = "TEST_P",
            Name = "TEST_P_NAME",
            DefaultCurrency = "USD",
            Priority = 1
        };
        context.StockPortfolios.Add(portfolio);
        await context.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);

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
        context.SaveChanges();

        var tranServ = new TransactionService(context, logger: testOutputHelper.ToLogger<TransactionService>());
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
