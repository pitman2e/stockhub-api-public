using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockHub.Models.CustomJsonConverter;

public class NullableIntegerConverter : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (string.IsNullOrWhiteSpace(reader.GetString()))
            {
                return null;
            }
            
            return Convert.ToInt32(reader.GetString());
        }

        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Value);
}