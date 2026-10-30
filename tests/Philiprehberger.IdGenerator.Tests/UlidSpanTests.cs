using System.Globalization;
using Xunit;
using Philiprehberger.IdGenerator;

namespace Philiprehberger.IdGenerator.Tests;

public class UlidSpanTests
{
    [Fact]
    public void TryParse_Span_RoundTripsToString()
    {
        var original = new Ulid();
        var str = original.ToString();

        Assert.True(Ulid.TryParse(str.AsSpan(), out var parsed));
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void TryParse_Span_IsCaseInsensitive()
    {
        var original = new Ulid();
        var lower = original.ToString().ToLowerInvariant();

        Assert.True(Ulid.TryParse(lower.AsSpan(), out var parsed));
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void TryParse_Span_FromSliceOfLargerBuffer()
    {
        var original = new Ulid();
        var text = $"id={original};";

        Assert.True(Ulid.TryParse(text.AsSpan(3, 26), out var parsed));
        Assert.Equal(original, parsed);
    }

    [Fact]
    public void TryParse_Span_WithWrongLengthOrInvalidChars_ReturnsFalse()
    {
        Assert.False(Ulid.TryParse(ReadOnlySpan<char>.Empty, out _));
        Assert.False(Ulid.TryParse(new string('A', 25).AsSpan(), out _));
        Assert.False(Ulid.TryParse(new string('A', 27).AsSpan(), out _));
        Assert.False(Ulid.TryParse("0000000000000000000000000U".AsSpan(), out _));
    }

    [Fact]
    public void Parse_Span_OnInvalidInput_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => Ulid.Parse("not-a-ulid-string-26-chars".AsSpan()));
    }

    [Fact]
    public void Parse_WithProvider_MatchesParse()
    {
        var str = new Ulid().ToString();

        Assert.Equal(Ulid.Parse(str), Ulid.Parse(str, CultureInfo.InvariantCulture));
        Assert.Equal(Ulid.Parse(str), Ulid.Parse(str.AsSpan(), CultureInfo.InvariantCulture));
        Assert.True(Ulid.TryParse(str, CultureInfo.InvariantCulture, out var a));
        Assert.True(Ulid.TryParse(str.AsSpan(), CultureInfo.InvariantCulture, out var b));
        Assert.Equal(a, b);
    }

    [Fact]
    public void TryFormat_WritesCanonicalString()
    {
        var ulid = new Ulid();
        Span<char> buffer = stackalloc char[32];

        Assert.True(ulid.TryFormat(buffer, out var written, default, null));
        Assert.Equal(26, written);
        Assert.Equal(ulid.ToString(), buffer[..written].ToString());
    }

    [Fact]
    public void TryFormat_WithTooSmallBuffer_ReturnsFalse()
    {
        var ulid = new Ulid();
        Span<char> buffer = stackalloc char[25];

        Assert.False(ulid.TryFormat(buffer, out var written, default, null));
        Assert.Equal(0, written);
    }

    [Fact]
    public void TryFormat_WithUnsupportedFormat_Throws()
    {
        var ulid = new Ulid();
        var buffer = new char[26];

        Assert.Throws<FormatException>(() => ulid.TryFormat(buffer, out _, "X", null));
    }

    [Fact]
    public void ToString_WithFormatAndProvider_MatchesToString()
    {
        var ulid = new Ulid();

        Assert.Equal(ulid.ToString(), ulid.ToString(null, CultureInfo.InvariantCulture));
        Assert.Equal(ulid.ToString(), ulid.ToString("G", null));
        Assert.Throws<FormatException>(() => ulid.ToString("x", null));
    }

    [Fact]
    public void Interpolation_UsesSpanFormatting()
    {
        var ulid = new Ulid();
        Assert.Equal($"usr_{ulid.ToString()}", $"usr_{ulid}");
    }

    [Fact]
    public void Default_FormatsAsAllZeros()
    {
        Assert.Equal(new string('0', 26), default(Ulid).ToString());
        Assert.Equal(Ulid.MinValue.ToString(), default(Ulid).ToString());
    }

    [Fact]
    public void MaxValue_RoundTripsThroughSpanApis()
    {
        Span<char> buffer = stackalloc char[26];
        Assert.True(Ulid.MaxValue.TryFormat(buffer, out _, default, null));
        Assert.Equal("7ZZZZZZZZZZZZZZZZZZZZZZZZZ", buffer.ToString());
        Assert.Equal(Ulid.MaxValue, Ulid.Parse(buffer));
    }

    [Fact]
    public void GenericParsable_WorksThroughInterface()
    {
        var str = new Ulid().ToString();
        Assert.Equal(Ulid.Parse(str), ParseGeneric<Ulid>(str));
    }

    private static T ParseGeneric<T>(string s) where T : ISpanParsable<T> =>
        T.Parse(s.AsSpan(), CultureInfo.InvariantCulture);
}
