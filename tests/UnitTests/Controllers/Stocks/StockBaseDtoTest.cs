using System;
using JetBrains.Annotations;
using StockHub.Controllers.Stocks;
using StockHub.Errors;
using Xunit;

namespace UnitTests.Controllers.Stocks;

[TestSubject(typeof(StockBaseDto))]
public class StockBaseDtoTest
{
    [Fact]
    public void GetDateMaturityDate_Parse_BasicDate()
    {
        var dto = new StockBaseDto
        {
            MaturityDate = "2026-01-02"
        };

        Assert.Equal(new DateOnly(2026, 1, 2), dto.GetDateMaturityDate());
    }
    
    [Theory]
    [InlineData("2026-21-02")]
    [InlineData("2026-01-02 00:12:00")]
    [InlineData("1767196800")]
    [InlineData("21-02-2026")]
    [InlineData("01/01/2026")]
    public void GetDateMaturityDate_Parse_InvalidateDate(string dateStr)
    {
        var dto = new StockBaseDto
        {
            MaturityDate = dateStr
        };

        var ex = Assert.Throws<SHArgumentException>(() => dto.GetDateMaturityDate());
        Assert.Equal(nameof(dto.MaturityDate), ex.FieldName);
    }
}