using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StockHub.Database;
using StockHub.Exchanges;
using StockHub.Exchanges.ConcreteExchanges;
using StockHub.Interfaces;
using StockHub.Models;
using StockHub.Services;
using StockHub.Services.Position;
using StockHub.Services.Transaction;
using Xunit;

namespace IntegrationTests.Transaction;

public class TransactionWriteWorkflowTests
{
    [Fact]
    public async Task CreateAsync_PersistsTransactionAndPosition()
    {
        await using var testScope = await TestScope.CreateAsync();
        var workflow = testScope.CreateWorkflow();
        var transactionDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var created = await workflow.CreateAsync(
            testScope.Uid,
            testScope.PortfolioId,
            testScope.StockId,
            new TransactionWriteData(
                2m,
                StockTransaction.TRANTYPE_BUY,
                null,
                null,
                null,
                null,
                null,
                transactionDate,
                10m,
                false));

        var savedTransaction = await testScope.Context.StockTransactions
            .AsNoTracking()
            .SingleAsync(
                transaction => transaction.Uid == testScope.Uid,
                TestContext.Current.CancellationToken);
        var savedPosition = await testScope.Context.StockPositions
            .AsNoTracking()
            .SingleAsync(
                position => position.Uid == testScope.Uid,
                TestContext.Current.CancellationToken);

        Assert.Equal(created.iden, savedTransaction.iden);
        Assert.Equal(testScope.PortfolioId, savedTransaction.PortfolioId);
        Assert.Equal(testScope.StockId, savedTransaction.StockId);
        Assert.Equal(2m, savedPosition.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_ChangesTransactionAndRefreshesPosition()
    {
        await using var testScope = await TestScope.CreateAsync();
        var workflow = testScope.CreateWorkflow();
        var transactionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var created = await workflow.CreateAsync(
            testScope.Uid,
            testScope.PortfolioId,
            testScope.StockId,
            CreateBuyData(transactionDate, 2m));

        var updated = await workflow.UpdateAsync(
            testScope.Uid,
            created.iden,
            created.Version,
            CreateBuyData(transactionDate, 3m));

        var savedPosition = await testScope.Context.StockPositions
            .AsNoTracking()
            .SingleAsync(
                position => position.Uid == testScope.Uid,
                TestContext.Current.CancellationToken);

        Assert.Equal(3m, updated.TxCount);
        Assert.Equal(3m, savedPosition.Quantity);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTransactionAndPosition()
    {
        await using var testScope = await TestScope.CreateAsync();
        var workflow = testScope.CreateWorkflow();
        var transactionDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var created = await workflow.CreateAsync(
            testScope.Uid,
            testScope.PortfolioId,
            testScope.StockId,
            CreateBuyData(transactionDate, 2m));

        var deleted = await workflow.DeleteAsync(testScope.Uid, created.iden);

        Assert.NotNull(deleted);
        Assert.False(await testScope.Context.StockTransactions
            .AsNoTracking()
            .AnyAsync(
                transaction => transaction.Uid == testScope.Uid,
                TestContext.Current.CancellationToken));
        Assert.False(await testScope.Context.StockPositions
            .AsNoTracking()
            .AnyAsync(
                position => position.Uid == testScope.Uid,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateAsync_RollsBackTransactionWhenPositionRefreshFails()
    {
        await using var testScope = await TestScope.CreateAsync();
        var workflow = testScope.CreateWorkflow();
        var transactionDate = DateOnly.FromDateTime(DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await workflow.CreateAsync(
                testScope.Uid,
                testScope.PortfolioId,
                testScope.StockId,
                CreateBuyData(transactionDate, -1m, StockTransaction.TRANTYPE_SELL)));

        Assert.False(await testScope.Context.StockTransactions
            .AsNoTracking()
            .AnyAsync(
                transaction => transaction.Uid == testScope.Uid,
                TestContext.Current.CancellationToken));
        Assert.False(await testScope.Context.StockMetadata
            .AsNoTracking()
            .AnyAsync(
                metadata => metadata.StockId == testScope.StockId,
                TestContext.Current.CancellationToken));
    }

    private static TransactionWriteData CreateBuyData(DateOnly transactionDate, decimal quantity, string transactionType = StockTransaction.TRANTYPE_BUY)
    {
        return new TransactionWriteData(
            quantity,
            transactionType,
            null,
            null,
            null,
            null,
            null,
            transactionDate,
            10m,
            false);
    }

    private sealed class TestScope : IAsyncDisposable
    {
        private readonly string _currencyId;
        private readonly bool _ownsCurrency;

        private TestScope(StockHubContext context, string uid, string portfolioId, string stockId, string currencyId, bool ownsCurrency)
        {
            Context = context;
            Uid = uid;
            PortfolioId = portfolioId;
            StockId = stockId;
            _currencyId = currencyId;
            _ownsCurrency = ownsCurrency;
        }

        public StockHubContext Context { get; }

        public string Uid { get; }

        public string PortfolioId { get; }

        public string StockId { get; }

        public static async Task<TestScope> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONSTR");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DATABASE_CONSTR must point to the isolated test PostgreSQL database.");
            }

            var context = new StockHubContext(connectionString);
            var uniqueId = Guid.NewGuid().ToString("N");
            var uid = uniqueId[..32];
            var portfolioId = $"T{uniqueId[..14]}";
            var stockId = $"T{uniqueId[..14]}.US";
            const string currencyId = CurrencyExchangeRate.HKD;
            var ownsCurrency = !await context.Currencies.AnyAsync(
                currency => currency.CurrencyId == currencyId,
                TestContext.Current.CancellationToken);

            if (ownsCurrency)
            {
                context.Currencies.Add(new Currency
                {
                    CurrencyId = currencyId,
                    CurrencyName = "Integration Test Currency",
                    ToUsdRate = 1m
                });
            }
            context.Stocks.Add(new Stock
            {
                StockId = stockId,
                StockName = "Integration Test Stock",
                Currency = currencyId,
                AssetClass = Stock.ASSET_CLASS_STOCK,
                Coupon = null,
                CouponFreq = null,
                MaturityDate = null,
                FaceValue = null
            });
            context.StockPortfolios.Add(new StockPortfolio
            {
                Uid = uid,
                PortfolioId = portfolioId,
                Name = "Integration Test Portfolio",
                Priority = 1,
                DefaultCurrency = currencyId,
                IsExcludedFromSummary = false,
                IsVirtual = false
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return new TestScope(context, uid, portfolioId, stockId, currencyId, ownsCurrency);
        }

        public TransactionWriteWorkflow CreateWorkflow()
        {
            var stock2ExchangeService = new Stock2ExchangeService(
                new AllExchanges([new CASH(), new US(null!)]));
            return new TransactionWriteWorkflow(
                Context,
                new TransactionService(Context, NullLogger<TransactionService>.Instance),
                new RealisedScripService(Context, new TestUserClaims()),
                new PositionValueService(Context),
                stock2ExchangeService);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.StockPositions
                .Where(position => position.Uid == Uid)
                .ExecuteDeleteAsync();
            await Context.StockRealisedScrips
                .Where(scrip => scrip.Uid == Uid)
                .ExecuteDeleteAsync();
            await Context.StockTransactions
                .Where(transaction => transaction.Uid == Uid)
                .ExecuteDeleteAsync();
            await Context.StockMetadata
                .Where(metadata => metadata.StockId == StockId)
                .ExecuteDeleteAsync();
            await Context.StockPortfolios
                .Where(portfolio => portfolio.Uid == Uid)
                .ExecuteDeleteAsync();
            await Context.Stocks
                .Where(stock => stock.StockId == StockId)
                .ExecuteDeleteAsync();
            if (_ownsCurrency)
            {
                await Context.Currencies
                    .Where(currency => currency.CurrencyId == _currencyId)
                    .ExecuteDeleteAsync();
            }
            await Context.DisposeAsync();
        }
    }

    private sealed class TestUserClaims : IUserClaims
    {
        public string GetUid() => "INTEGRATION_TEST_UID";
    }
}