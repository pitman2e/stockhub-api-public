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
[TestSubject(typeof(StockPostDtoValidator))]
public class StockPostDtoValidatorTest
{
    private readonly StockHubContext _dbContext;
    private readonly Mock<Stock2ExchangeService> _exchangeServiceMock;
    private readonly StockPostDtoValidator _validator;

    public StockPostDtoValidatorTest()
    {
        var options = new DbContextOptionsBuilder<StockHubContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new StockHubContext(options);

        var allExchangesMock = AllExchangesMock.Get();
        _exchangeServiceMock = new Mock<Stock2ExchangeService>(allExchangesMock);

        _validator = new StockPostDtoValidator(_exchangeServiceMock.Object, _dbContext);
    }

    [Fact]
    public async Task Validate_WhenStockIdAlreadyExists_ShouldHaveValidationError()
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

        var dto = new StockPostDto { stockId = "AAPL.US" };

        var result = await _validator.TestValidateAsync(dto, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.stockId)
              .WithErrorMessage("Stock Id already exist");
    }

    [Fact]
    public async Task Validate_WhenStockIdFormatIsInvalid_ShouldHaveValidationError()
    {
        var dto = new StockPostDto { stockId = "INVALID_ID" };

        var result = await _validator.TestValidateAsync(dto, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldHaveValidationErrorFor(x => x.stockId)
              .WithErrorMessage("Invalid Stock Id format");
    }

    [Fact]
    public async Task Validate_WhenStockPostDtoIsValid_ShouldNotHaveValidationError()
    {
        var dto = new StockPostDto { stockId = "MSFT.US" };

        var result = await _validator.TestValidateAsync(dto, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldNotHaveValidationErrorFor(x => x.stockId);
    }
}