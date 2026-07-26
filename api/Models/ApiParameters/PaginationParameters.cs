using System.ComponentModel;

namespace StockHub.Models.ApiParameters;

// [property: DefaultValue] is read by Swagger, the actual default value set is read by runtime
public record PaginationParameters(
    [property: Description("The index of the first result")]
    [property: DefaultValue(0)]
    int Offset = 0,

    [property: Description("The number of results to return")]
    [property: DefaultValue(25)]
    int Limit = 25);