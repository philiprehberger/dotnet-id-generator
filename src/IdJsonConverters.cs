using System.Text.Json;
using System.Text.Json.Serialization;

namespace Philiprehberger.IdGenerator;

/// <summary>
/// A <see cref="JsonConverter{T}"/> for serializing and deserializing <see cref="Ulid"/> values
/// as their 26-character Crockford Base32 string representation.
/// </summary>
public sealed class UlidJsonConverter : JsonConverter<Ulid>
{
    /// <inheritdoc />
    public override Ulid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (str is null)
            throw new JsonException("Expected a ULID string, got null.");

        if (!Ulid.TryParse(str, out var ulid))
            throw new JsonException($"Invalid ULID string: '{str}'");

        return ulid;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Ulid value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
