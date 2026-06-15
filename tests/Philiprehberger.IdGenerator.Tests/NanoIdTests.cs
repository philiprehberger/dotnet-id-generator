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
}
