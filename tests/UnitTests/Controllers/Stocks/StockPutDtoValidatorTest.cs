using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockHub.Controllers.Stocks;
using StockHub.Database;
using StockHub.Services;
using UnitTests.Mocks;
using Xunit;

namespace UnitTests.Controllers.Stocks;

// [AI-Generated]
[TestSubject(typeof(StockPutDtoValidator))]
public class StockPutDtoValidatorTest
{
    private readonly StockHubContext _dbContext;
    private readonly Mock<Stock2ExchangeService> _exchangeServiceMock;
    private readonly StockPutDtoValidator _validator;

    public StockPutDtoValidatorTest()
    {
        var options = new DbContextOptionsBuilder<StockHubContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new StockHubContext(options);

        var allExchangesMock = AllExchangesMock.Get();
        _exchangeServiceMock = new Mock<Stock2ExchangeService>(allExchangesMock);

        _validator = new StockPutDtoValidator(_exchangeServiceMock.Object, _dbContext);
    }

    [Fact]
    public async Task Validate_WhenStockDoesNotExist_ShouldHaveValidationError()
    {
        var dto = new StockPutDto { KeyStockId = "UNKNOWN.US", Version = 1 };

        var result = await _validator.TestValidateAsync(dto, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.StockId)
              .WithErrorMessage("Stock Id does not exist");
    }

    [Fact]
    public async Task Validate_WhenStockIdFormatIsInvalid_ShouldHaveValidationError()
    {
        _dbContext.Stocks.Add(new Stock
        {
            StockId = "BAD.FORMAT",
            StockName = "Test Stock",
            Currency = "USD",
            AssetClass = "STOCK",
            Coupon = null,
            CouponFreq = null,
            MaturityDate = null,
            FaceValue = null
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new StockPutDto { KeyStockId = "BAD.FORMAT", Version = 1 };

        var result = await _validator.TestValidateAsync(dto, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.StockId)
              .WithErrorMessage("Invalid Stock Id format");
    }

    [Fact]
    public async Task Validate_WhenVersionIsZero_ShouldHaveValidationError()
    {
        _dbContext.Stocks.Add(new Stock
        {
            StockId = "AAPL.US",
            StockName = "Apple Inc.",
            Currency = "USD",
            AssetClass = "STOCK",
            Coupon = null,
            CouponFreq = null,
            MaturityDate = null,
            FaceValue = null
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new StockPutDto { KeyStockId = "AAPL.US", Version = 0 };

        var result = await _validator.TestValidateAsync(dto, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.Version)
              .WithErrorMessage("Row version is required");
    }

    [Fact]
    public async Task Validate_WhenStockPutDtoIsValid_ShouldNotHaveValidationError()
    {
        _dbContext.Stocks.Add(new Stock
        {
            StockId = "AAPL.US",
            StockName = "Apple Inc.",
            Currency = "USD",
            AssetClass = "STOCK",
            Coupon = null,
            CouponFreq = null,
            MaturityDate = null,
            FaceValue = null
        });
        await _dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new StockPutDto { KeyStockId = "AAPL.US", Version = 1 };

        var result = await _validator.TestValidateAsync(dto, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveValidationErrorFor(x => x.StockId);
        result.ShouldNotHaveValidationErrorFor(x => x.Version);
    }
}