using System.Collections.Concurrent;

namespace TeReoLocalizer.Shared.Code;

using System.Text.Json;
using System.Text.Json.Serialization;

public class SortedDictionaryConverter<TKey, TValue> : JsonConverter<ConcurrentDictionary<TKey, TValue>> where TKey : notnull
{
    public override ConcurrentDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ConcurrentDictionary<TKey, TValue> dictionary = new ConcurrentDictionary<TKey, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name.");
            }

            string? propertyName = reader.GetString();

            reader.Read();

            TKey? key;

            if (typeof(TKey) == typeof(string))
            {
                key = (TKey?)(object?)propertyName;
            }
            else
            {
                key = JsonSerializer.Deserialize<TKey>(propertyName, options);
            }

            TValue? value = JsonSerializer.Deserialize<TValue>(ref reader, options);

            if (key is not null)
            {
                dictionary[key] = value;
            }
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, ConcurrentDictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<TKey, TValue> kvp in value.OrderBy(x => x.Key))
        {
            writer.WritePropertyName(kvp.Key.ToString() ?? string.Empty);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }

        writer.WriteEndObject();
    }
}
