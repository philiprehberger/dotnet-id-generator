using Xunit;
using Philiprehberger.IdGenerator;

namespace Philiprehberger.IdGenerator.Tests;

public class IdTests
{
    [Fact]
    public void NewUlid_ReturnsDistinctValues()
    {
        var a = Id.NewUlid();
        var b = Id.NewUlid();
        Assert.NotEqual(a.ToString(), b.ToString());
    }

    [Fact]
    public void NewNanoId_ReturnsDefaultLength()
    {
        var id = Id.NewNanoId();
        Assert.Equal(21, id.Length);
    }

    [Fact]
    public void NewNanoId_RespectsCustomSizeAndAlphabet()
    {
        var id = Id.NewNanoId(size: 10, alphabet: "0123456789abcdef");
        Assert.Equal(10, id.Length);
        Assert.All(id, c => Assert.Contains(c, "0123456789abcdef"));
    }

    [Fact]
    public void NewPrefixed_PrependsPrefixWithUnderscore()
    {
        var id = Id.NewPrefixed("usr");
        Assert.StartsWith("usr_", id);
        Assert.Equal("usr_".Length + 26, id.Length);
    }

    [Fact]
    public void NewShortId_DefaultsToTwelveChars()
    {
        var id = Id.NewShortId();
        Assert.Equal(12, id.Length);
    }

    [Fact]
    public void NewShortId_AcceptsCustomLength()
    {
        var id = Id.NewShortId(8);
        Assert.Equal(8, id.Length);
    }

    [Fact]
    public void NewMonotonicUlid_ReturnsStrictlyIncreasingValues()
    {
        var prev = Id.NewMonotonicUlid();
        for (var i = 0; i < 100; i++)
        {
            var next = Id.NewMonotonicUlid();
            Assert.True(next > prev, $"Iteration {i}: expected next > prev");
            prev = next;
        }
    }

    [Fact]
    public void NewGuidV7_HasVersionSevenAndRfcVariant()
    {
        var guid = Id.NewGuidV7();
        var bytes = guid.ToByteArray(bigEndian: true);

        Assert.Equal(0x70, bytes[6] & 0xF0);
        Assert.Equal(0x80, bytes[8] & 0xC0);
    }
}
