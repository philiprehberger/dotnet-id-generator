using Xunit;
using Philiprehberger.IdGenerator;

namespace Philiprehberger.IdGenerator.Tests;

public class NanoIdTests
{
    [Fact]
    public void Generate_DefaultSize_ReturnsTwentyOneChars()
    {
        Assert.Equal(21, NanoId.Generate().Length);
    }

    [Fact]
    public void Generate_UsesDefaultAlphabet()
    {
        var id = NanoId.Generate(size: 100);
        Assert.All(id, c => Assert.Contains(c, NanoId.DefaultAlphabet));
    }

    [Fact]
    public void Generate_RespectsCustomAlphabet()
    {
        var id = NanoId.Generate(size: 50, alphabet: "ab");
        Assert.All(id, c => Assert.Contains(c, "ab"));
    }

    [Fact]
    public void Generate_RespectsCustomSize()
    {
        Assert.Equal(5, NanoId.Generate(5).Length);
        Assert.Equal(100, NanoId.Generate(100).Length);
    }

    [Fact]
    public void Generate_WithZeroSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NanoId.Generate(size: 0));
    }

    [Fact]
    public void Generate_WithEmptyAlphabet_Throws()
    {
        Assert.Throws<ArgumentException>(() => NanoId.Generate(alphabet: ""));
    }

    [Fact]
    public void Generate_ProducesDistinctValuesOnRepeatedCalls()
    {
        var first = NanoId.Generate();
        var second = NanoId.Generate();
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Alphabets_Default_EqualsDefaultAlphabet()
    {
        Assert.Equal(NanoId.DefaultAlphabet, NanoId.Alphabets.Default);
    }

    [Fact]
    public void Alphabets_HaveNoDuplicateCharacters()
    {
        foreach (var alphabet in new[]
        {
            NanoId.Alphabets.Default,
            NanoId.Alphabets.Numbers,
            NanoId.Alphabets.Lowercase,
            NanoId.Alphabets.Alphanumeric,
            NanoId.Alphabets.NoLookAlikes,
        })
        {
            Assert.Equal(alphabet.Length, alphabet.Distinct().Count());
        }
    }

    [Fact]
    public void Alphabets_NoLookAlikes_ExcludesAmbiguousCharacters()
    {
        foreach (var c in "1lI0OouvSs2Z5")
            Assert.DoesNotContain(c, NanoId.Alphabets.NoLookAlikes);
    }

    [Fact]
    public void Generate_WithNumbersAlphabet_ProducesOnlyDigits()
    {
        var id = NanoId.Generate(size: 100, alphabet: NanoId.Alphabets.Numbers);
        Assert.All(id, c => Assert.True(char.IsAsciiDigit(c)));
    }

    [Fact]
    public void Generate_WithAlphanumericAlphabet_ProducesNoSymbols()
    {
        var id = NanoId.Generate(size: 200, alphabet: NanoId.Alphabets.Alphanumeric);
        Assert.All(id, c => Assert.True(char.IsAsciiLetterOrDigit(c)));
    }
}
