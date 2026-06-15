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
}
