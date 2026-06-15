using System.Text.Json;
using Xunit;
using Philiprehberger.IdGenerator;

namespace Philiprehberger.IdGenerator.Tests;

public class UlidJsonConverterTests
{
    private static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new UlidJsonConverter());
        return options;
    }

    [Fact]
    public void Serialize_EmitsQuotedUlidString()
    {
        var options = BuildOptions();
        var ulid = new Ulid();

        var json = JsonSerializer.Serialize(ulid, options);

        Assert.Equal($"\"{ulid}\"", json);
    }

    [Fact]
    public void Deserialize_RoundTripsThroughString()
    {
        var options = BuildOptions();
        var original = new Ulid();
        var json = JsonSerializer.Serialize(original, options);

        var roundtrip = JsonSerializer.Deserialize<Ulid>(json, options);

        Assert.Equal(original, roundtrip);
    }

    [Fact]
    public void Deserialize_NullValue_Throws()
    {
        var options = BuildOptions();
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Ulid>("null", options));
    }

    [Fact]
    public void Deserialize_InvalidString_Throws()
    {
        var options = BuildOptions();
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<Ulid>("\"not-a-valid-ulid-string!!\"", options));
    }
}
