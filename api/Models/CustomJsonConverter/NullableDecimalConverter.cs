using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockHub.Models.CustomJsonConverter;

public class NullableDecimalConverter : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (string.IsNullOrWhiteSpace(reader.GetString()))
            {
                return null;
            }
            
            return Convert.ToDecimal(reader.GetString());
        }

        return reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}