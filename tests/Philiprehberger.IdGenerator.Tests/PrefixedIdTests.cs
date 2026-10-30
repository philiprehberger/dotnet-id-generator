using Xunit;
using Philiprehberger.IdGenerator;

namespace Philiprehberger.IdGenerator.Tests;

public class PrefixedIdTests
{
    [Fact]
    public void Create_StartsWithPrefixAndUnderscore()
    {
        var id = PrefixedId.Create("usr");
        Assert.StartsWith("usr_", id);
    }

    [Fact]
    public void Create_AppendsValidUlid()
    {
        var id = PrefixedId.Create("usr");
        var ulidPart = id["usr_".Length..];
        Assert.True(Ulid.TryParse(ulidPart, out _));
    }

    [Fact]
    public void Create_WithEmptyPrefix_Throws()
    {
        Assert.Throws<ArgumentException>(() => PrefixedId.Create(""));
        Assert.Throws<ArgumentException>(() => PrefixedId.Create("   "));
    }

    [Fact]
    public void Validate_WithMatchingPrefixAndValidUlid_ReturnsTrue()
    {
        var id = PrefixedId.Create("org");
        Assert.True(PrefixedId.Validate(id, "org"));
    }

    [Fact]
    public void Validate_WithWrongPrefix_ReturnsFalse()
    {
        var id = PrefixedId.Create("usr");
        Assert.False(PrefixedId.Validate(id, "org"));
    }

    [Fact]
    public void Validate_WithInvalidUlid_ReturnsFalse()
    {
        Assert.False(PrefixedId.Validate("usr_not-a-valid-ulid-here", "usr"));
    }

    [Fact]
    public void Validate_WithEmptyInputs_ReturnsFalse()
    {
        Assert.False(PrefixedId.Validate("", "usr"));
        Assert.False(PrefixedId.Validate("usr_01HXYZ", ""));
    }

    [Fact]
    public void TryParse_RoundTripsCreate()
    {
        var id = PrefixedId.Create("usr");

        Assert.True(PrefixedId.TryParse(id, out var prefix, out var ulid));
        Assert.Equal("usr", prefix);
        Assert.Equal(id, $"{prefix}_{ulid}");
    }

    [Fact]
    public void TryParse_PrefixContainingUnderscore_SplitsAtLastUnderscore()
    {
        var id = PrefixedId.Create("acct_live");

        Assert.True(PrefixedId.TryParse(id, out var prefix, out _));
        Assert.Equal("acct_live", prefix);
        Assert.True(PrefixedId.Validate(id, prefix));
    }

    [Fact]
    public void TryParse_IsCaseInsensitiveForUlidPart()
    {
        var ulid = new Ulid();

        Assert.True(PrefixedId.TryParse($"usr_{ulid.ToString().ToLowerInvariant()}", out _, out var parsed));
        Assert.Equal(ulid, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("_01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("   _01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("usr_")]
    [InlineData("usr_not-a-valid-ulid-here")]
    [InlineData("usr-01ARZ3NDEKTSV4RRFFQ69G5FAV")]
    public void TryParse_WithInvalidInput_ReturnsFalse(string? id)
    {
        Assert.False(PrefixedId.TryParse(id, out var prefix, out var ulid));
        Assert.Equal(string.Empty, prefix);
        Assert.Equal(default, ulid);
    }
}
