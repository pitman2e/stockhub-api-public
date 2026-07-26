using System;
using System.Linq;
using System.Text.Json;

namespace StockHub.Errors;

public class HookError
{
    public HookError(string fieldName, string message)
    {
        FieldName = fieldName;
        Message = message;
    }

    public string FieldName
    {
        get;
        set => field = !string.IsNullOrEmpty(value)
            ? ToCamelCase(value)
            : value;
    }

    public string Message { get; set; }

    private static string ToCamelCase(string value)
    {
        if (!value.Contains('_')) return JsonNamingPolicy.CamelCase.ConvertName(value);
        var words = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select((w, i) =>
            i == 0
                ? char.ToLowerInvariant(w[0]) + w[1..]
                : char.ToUpperInvariant(w[0]) + w[1..]));

    }
}