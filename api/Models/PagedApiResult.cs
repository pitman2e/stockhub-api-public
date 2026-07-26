using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockHub.Models;

public class PagedApiResult<T>
{
    [JsonPropertyName("tableData")]
    public IEnumerable<T> TableData { get; set; }
    
    [JsonPropertyName("pageNo")]
    public int PageNo { get; set; }
    
    [JsonPropertyName("rowsPerPage")]
    public int RowsPerPage { get; set; }
    
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}