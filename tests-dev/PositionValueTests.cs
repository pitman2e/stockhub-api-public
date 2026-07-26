using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockHub.Repositories;
using StockHub.Services;
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
        var userClaim = TestUserClaim.Get();
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
                Assert.True(poss[posIdx].StockId == poss1[posIdx].StockId);
                Assert.True(poss[posIdx].PortfolioId == poss1[posIdx].PortfolioId);
                Assert.True(Math.Round(poss[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero) == Math.Round(poss1[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].Quantity, 0, MidpointRounding.AwayFromZero) == Math.Round(poss1[posIdx].Quantity, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero) == Math.Round(poss1[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero));
            }
        }
    }
        
    [Theory]
    [MemberData(nameof(Data))]
    public async Task V2IsNotUseCachePosTest_Reverse(DateOnly dateToDate, int loopCnt, int interval)
    {
        var context = TestStockHubContext.Get();
        var userClaim = TestUserClaim.Get();
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
                Assert.True(poss[posIdx].StockId == poss1[posIdx].StockId);
                Assert.True(poss[posIdx].PortfolioId == poss1[posIdx].PortfolioId);
                Assert.True(Math.Round(poss[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero) == Math.Round(poss1[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].Quantity, 0, MidpointRounding.AwayFromZero) == Math.Round(poss1[posIdx].Quantity, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero) == Math.Round(poss1[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero));
            }
        }
    }
        
    [Theory]
    [MemberData(nameof(Data))]
    public async Task V1V2Test(DateOnly dateToDate, int loopCnt, int interval)
    {
        var context = TestStockHubContext.Get();
        /*var userClaim = TestUserClaim.Get();
        var tranServ = new TransactionService(context, logger: null);*/ 
        var posValServ = new PositionValueService(context);
        var tranRepo = new TransactionRepo(context);
        var posValServV1 = new PositionValueServiceV1(context, tranRepo);

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
            var poss = (await
                posValServ.GetStockPositionValuesAsync(
                        upsFilter,
                        pDateFmDate,
                        pDateToDate,
                        isSkipNonmarketDate: false,
                        PositionValueService.PositionStatus.Any,
                        isNotUseCachePos: false
                    ))
                    .OrderBy(x => x.Uid)
                    .ThenBy(x => x.PortfolioId)
                    .ThenBy(x => x.StockId)
                    .ThenBy(x => x.ObserveDate)
                    .ToList();

            watch.Stop();
            var elapsedV2 = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedV2)} : {elapsedV2} ms");
                
            watch.Restart();
            var poss2 =
                posValServV1.GetStockPositionValues(
                        upsFilter,
                        pDateFmDate,
                        pDateToDate,
                        isSkipNonmarketDate: false,
                        PositionValueService.PositionStatus.Any
                    )
                    .OrderBy(x => x.Uid)
                    .ThenBy(x => x.PortfolioId)
                    .ThenBy(x => x.StockId)
                    .ThenBy(x => x.ObserveDate)
                    .ToList();
                
            watch.Stop();
            var elapsedV1 = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedV1)} : {elapsedV1} ms");
                
            Assert.True(poss.Count == poss2.Count);

            for (var posIdx = 0; posIdx < poss.Count; posIdx++)
            {
                Assert.True(poss[posIdx].StockId == poss2[posIdx].StockId);
                Assert.True(poss[posIdx].PortfolioId == poss2[posIdx].PortfolioId);
                Assert.True(Math.Round(poss[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero) ==
                            Math.Round(poss2[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].Quantity, 0, MidpointRounding.AwayFromZero) ==
                            Math.Round(poss2[posIdx].Quantity, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero) ==
                            Math.Round(poss2[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero));
            }
        }
    }
        
    [Theory]
    [MemberData(nameof(Data))]
    public async Task V1V2TestReverse(DateOnly dateToDate, int loopCnt, int interval)
    {
        var context = TestStockHubContext.Get();
        var userClaim = TestUserClaim.Get();
        var tranServ = new TransactionService(context, logger: null);
        var tranRepo = new TransactionRepo(context);
        var posValServ = new PositionValueService(context);
        var posValServV1 = new PositionValueServiceV1(context, tranRepo);

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
            var poss2 =
                posValServV1.GetStockPositionValues(
                        upsFilter,
                        pDateFmDate,
                        pDateToDate,
                        isSkipNonmarketDate: false,
                        PositionValueService.PositionStatus.Any
                    )
                    .OrderBy(x => x.Uid)
                    .ThenBy(x => x.PortfolioId)
                    .ThenBy(x => x.StockId)
                    .ThenBy(x => x.ObserveDate)
                    .ToList();
                
            watch.Stop();
            var elapsedV1 = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedV1)} : {elapsedV1} ms");
                
            watch.Restart();
            var poss = (await
                posValServ.GetStockPositionValuesAsync(
                        upsFilter,
                        pDateFmDate,
                        pDateToDate,
                        isSkipNonmarketDate: false,
                        PositionValueService.PositionStatus.Any,
                        isNotUseCachePos: false
                    ))
                    .OrderBy(x => x.Uid)
                    .ThenBy(x => x.PortfolioId)
                    .ThenBy(x => x.StockId)
                    .ThenBy(x => x.ObserveDate)
                    .ToList();

            watch.Stop();
            var elapsedV2 = watch.ElapsedMilliseconds;
            testOutputHelper.WriteLine($"{nameof(elapsedV2)} : {elapsedV2} ms");

            Assert.True(poss.Count == poss2.Count);
            for (var posIdx = 0; posIdx < poss.Count; posIdx++)
            {
                Assert.True(poss[posIdx].StockId == poss2[posIdx].StockId);
                Assert.True(poss[posIdx].PortfolioId == poss2[posIdx].PortfolioId);
                Assert.True(Math.Round(poss[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero) ==
                            Math.Round(poss2[posIdx].TotalGain, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].Quantity, 0, MidpointRounding.AwayFromZero) ==
                            Math.Round(poss2[posIdx].Quantity, 0, MidpointRounding.AwayFromZero));
                Assert.True(Math.Round(poss[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero) ==
                            Math.Round(poss2[posIdx].TotalCost, 0, MidpointRounding.AwayFromZero));
            }
        }
    }
}