using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StockHub.Services.Position;
using StockHub.Tools;
using Xunit;

namespace UnitTestsDev;

public class PositionValueTests(ITestOutputHelper testOutputHelper)
{
    public static IEnumerable<object[]> Data =>
        new List<object[]> 
        { 
            //new object[] { new DateOnly(2026, 3, 26), 1, 365 * 6},
            new object[] { new DateOnly(2026, 3, 26), 24, 30 * 2},
            //new object[] { new DateOnly(2026, 3, 26), 12, 180},
            //new object[] { new DateOnly(2026, 3, 26), 6, 365},
        };
        
    [Theory]
    [MemberData(nameof(Data))]
    public async Task V2IsNotUseCachePosTest(DateOnly dateToDate, int loopCnt, int interval)
    {
        var context = TestStockHubContext.Get();
        var posValServ = new PositionValueService(context);

        var upsFilter = new UPSFilter(nullableUid: true, nullablePortfolioId: true, nullableStockId: true)
        {
            Uid = "",
            PortfolioId = "",
            StockId = ""
        };

        for(var i = 0; i < loopCnt; i++)
        {
            var pDateFmDate = dateToDate.AddDays(-interval * (i+1));
            var pDateToDate = dateToDate.AddDays(-interval * i);
                
            var watch = System.Diagnostics.Stopwatch.StartNew();

            watch.Restart();
            var poss= await
                posValServ.GetStockPositionValuesAsync(
                    upsFilter, 
                    pDateFmDate,
                    pDateToDate,
                    isSkipNonmarketDate: false,
                    PositionValueService.PositionStatus.Any,
                    isNotUseCachePos: true
                );

            watch.Stop();
            var elapsedNonCachePos = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedNonCachePos)} : {elapsedNonCachePos} ms");
                
            watch.Restart();
            var poss1= await
                posValServ.GetStockPositionValuesAsync(
                    upsFilter, 
                    pDateFmDate,
                    pDateToDate,
                    isSkipNonmarketDate: false,
                    PositionValueService.PositionStatus.Any,
                    isNotUseCachePos: false
                );
                
            watch.Stop();
            var elapsedCachePos = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedCachePos)} : {elapsedCachePos} ms");
                
            Assert.True(poss.Count == poss1.Count);
            for (var posIdx = 0; posIdx < poss.Count; posIdx++)
            {
                var pos1 = poss[posIdx];
                var pos2 = poss1[posIdx];
                
                Assert.Equal(pos1.StockId, pos2.StockId);
                Assert.Equal(pos1.PortfolioId, pos2.PortfolioId);
                Assert.Equal(pos1.ObserveDate, pos2.ObserveDate);
                Assert.Equal(pos1.MarketDate, pos2.MarketDate);
                Assert.Equal(Math.Round(pos1.StockPrice.GetValueOrDefault(), 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.StockPrice.GetValueOrDefault(), 0, MidpointRounding.AwayFromZero));
                Assert.Equal(Math.Round(pos1.TotalGain, 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.TotalGain, 0, MidpointRounding.AwayFromZero));
                Assert.Equal(Math.Round(pos1.Quantity, 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.Quantity, 0, MidpointRounding.AwayFromZero));
                Assert.Equal(Math.Round(pos1.TotalCost, 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.TotalCost, 0, MidpointRounding.AwayFromZero));
            }
        }
    }
        
    [Theory]
    [MemberData(nameof(Data))]
    public async Task V2IsNotUseCachePosTest_Reverse(DateOnly dateToDate, int loopCnt, int interval)
    {
        var context = TestStockHubContext.Get();
        var posValServ = new PositionValueService(context);

        var upsFilter = new UPSFilter(nullableUid: true, nullablePortfolioId: true, nullableStockId: true)
        {
            Uid = "",
            PortfolioId = "",
            StockId = ""
        };

        for(var i = 0; i < loopCnt; i++)
        {
            var pDateFmDate = dateToDate.AddDays(-interval * (i+1));
            var pDateToDate = dateToDate.AddDays(-interval * i);
                
            var watch = System.Diagnostics.Stopwatch.StartNew();

            watch.Restart();
            var poss1 = await posValServ.GetStockPositionValuesAsync(
                upsFilter,
                pDateFmDate,
                pDateToDate,
                isSkipNonmarketDate: false,
                PositionValueService.PositionStatus.Any,
                isNotUseCachePos: false
            );
                
            watch.Stop();
            var elapsedCachePos = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedCachePos)} : {elapsedCachePos} ms");
                
            watch.Restart();
            var poss = await posValServ.GetStockPositionValuesAsync(
                upsFilter,
                pDateFmDate,
                pDateToDate,
                isSkipNonmarketDate: false,
                PositionValueService.PositionStatus.Any,
                isNotUseCachePos: true
            );

            watch.Stop();
            var elapsedNonCachePos = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedNonCachePos)} : {elapsedNonCachePos} ms");

            Assert.True(poss.Count == poss1.Count);
            testOutputHelper.WriteLine($"Record count is {poss.Count}");

            for (var posIdx = 0; posIdx < poss.Count; posIdx++)
            {
                var pos1 = poss[posIdx];
                var pos2 = poss1[posIdx];
                
                Assert.Equal(pos1.StockId, pos2.StockId);
                Assert.Equal(pos1.PortfolioId, pos2.PortfolioId);
                Assert.Equal(pos1.ObserveDate, pos2.ObserveDate);
                Assert.Equal(pos1.MarketDate, pos2.MarketDate);
                Assert.Equal(Math.Round(pos1.StockPrice.GetValueOrDefault(), 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.StockPrice.GetValueOrDefault(), 0, MidpointRounding.AwayFromZero));
                Assert.Equal(Math.Round(pos1.TotalGain, 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.TotalGain, 0, MidpointRounding.AwayFromZero));
                Assert.Equal(Math.Round(pos1.Quantity, 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.Quantity, 0, MidpointRounding.AwayFromZero));
                Assert.Equal(Math.Round(pos1.TotalCost, 0, MidpointRounding.AwayFromZero),
                    Math.Round(pos2.TotalCost, 0, MidpointRounding.AwayFromZero));
            }
        }
    }
}