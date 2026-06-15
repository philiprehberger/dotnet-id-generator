using Xunit;
using Philiprehberger.IdGenerator;

namespace Philiprehberger.IdGenerator.Tests;

public class UlidTests
{
    [Fact]
    public void DefaultConstructor_ProducesTwentySixCharString()
    {
        var ulid = new Ulid();
        Assert.Equal(26, ulid.ToString().Length);
    }

    [Fact]
    public void Timestamp_IsCloseToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var ulid = new Ulid();
        var after = DateTimeOffset.UtcNow;

        Assert.InRange(ulid.Timestamp, before.AddMilliseconds(-1), after.AddMilliseconds(1));
    }

    [Fact]
    public void TryParse_RoundTripsToString()
    {
        var original = new Ulid();
        var str = original.ToString();

        Assert.True(Ulid.TryParse(str, out var parsed));
        Assert.Equal(str, parsed.ToString());
    }

    [Fact]
    public void TryParse_WithNullOrWrongLength_ReturnsFalse()
    {
        Assert.False(Ulid.TryParse(null, out _));
        Assert.False(Ulid.TryParse("", out _));
        Assert.False(Ulid.TryParse("toolong" + new string('A', 26), out _));
        Assert.False(Ulid.TryParse(new string('A', 25), out _));
    }

    [Fact]
    public void Parse_OnInvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Ulid.Parse("not-a-ulid-string-26-chars"));
    }

    [Fact]
    public void Ordering_ReflectsCreationTime()
    {
        var a = new Ulid();
        Thread.Sleep(2);
        var b = new Ulid();

        Assert.True(a < b);
        Assert.True(b > a);
        Assert.True(a <= b);
        Assert.True(b >= a);
    }

    [Fact]
    public void Equality_TwoSeparateInstancesWithSameStringAreEqual()
    {
        var ulid = new Ulid();
        var roundtrip = Ulid.Parse(ulid.ToString());

        Assert.True(ulid == roundtrip);
        Assert.False(ulid != roundtrip);
        Assert.Equal(ulid, roundtrip);
        Assert.Equal(ulid.GetHashCode(), roundtrip.GetHashCode());
    }

    [Fact]
    public void ToGuid_RoundTripsTo16Bytes()
    {
        var ulid = new Ulid();
        var guid = ulid.ToGuid();
        Assert.Equal(16, guid.ToByteArray().Length);
    }

    [Fact]
    public void NewMonotonic_ProducesStrictlyIncreasingValues()
    {
        var values = new List<Ulid>();
        for (var i = 0; i < 1000; i++)
            values.Add(Ulid.NewMonotonic());

        for (var i = 1; i < values.Count; i++)
            Assert.True(values[i] > values[i - 1], $"Pair {i} broke ordering");
    }

    [Fact]
    public void FromBytes_RoundTripsViaToByteArray()
    {
        var original = new Ulid();
        var bytes = original.ToByteArray();
        var restored = Ulid.FromBytes(bytes);

        Assert.Equal(original, restored);
        Assert.Equal(original.ToString(), restored.ToString());
    }

    [Fact]
    public void FromBytes_WithWrongLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => Ulid.FromBytes(new byte[15]));
        Assert.Throws<ArgumentException>(() => Ulid.FromBytes(new byte[17]));
    }

    [Fact]
    public void ToByteArray_ReturnsSixteenBytes()
    {
        var ulid = new Ulid();
        Assert.Equal(16, ulid.ToByteArray().Length);
    }
}
