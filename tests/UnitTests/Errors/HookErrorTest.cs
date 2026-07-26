using JetBrains.Annotations;
using StockHub.Errors;
using Xunit;

namespace UnitTests.Errors;

[TestSubject(typeof(HookError))]
public class HookErrorTest
{
    [Theory]
    [InlineData("TxCount", "txCount")]
    [InlineData("PortfolioId", "portfolioId")]
    [InlineData("tranType", "tranType")]
    [InlineData("", "")]
    [InlineData(null, null)]
    [InlineData("stock_id", "stockId")]
    [InlineData("Stock_Id", "stockId")]
    public void FieldName_ShouldBeConvertedToCamelCase(string? inputFieldName, string? expectedFieldName)
    {
        // Act
        var hookError = new HookError(inputFieldName!, "Test message");

        // Assert
        Assert.Equal(expectedFieldName, hookError.FieldName);
    }

    [Fact]
    public void Message_ShouldStoreValueCorrectly()
    {
        // Arrange & Act
        var hookError = new HookError("PortfolioId", "Invalid portfolio ID");

        // Assert
        Assert.Equal("Invalid portfolio ID", hookError.Message);
    }

    [Fact]
    public void FieldName_ShouldBeConvertedToCamelCase_WhenUpdatedViaPropertySetter()
    {
        // Arrange
        var hookError = new HookError("InitialField", "Test message");

        // Act
        hookError.FieldName = "NewPropertyName";

        // Assert
        Assert.Equal("newPropertyName", hookError.FieldName);
    }
}