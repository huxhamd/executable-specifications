namespace acceptance.Core.Dsl.UnitTests;

public class AliasTests
{
    [Fact]
    public void TheSameNameGivesTheSameAliasWithinASpec()
    {
        var alias = new Alias();

        Assert.Equal(alias.For("Buy milk"), alias.For("Buy milk"));
    }

    [Fact]
    public void TheSameNameGivesADifferentAliasInEachSpec()
    {
        Assert.NotEqual(new Alias().For("Buy milk"), new Alias().For("Buy milk"));
    }

    [Fact]
    public void AnAliasStartsWithTheNameItStandsFor()
    {
        Assert.StartsWith("Buy milk_", new Alias().For("Buy milk"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankNameIsLeftUnchanged(string name)
    {
        Assert.Equal(name, new Alias().For(name));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(256)]
    public void AnAliasOfLengthIsExactlyThatLong(int length)
    {
        Assert.Equal(length, new Alias().OfLength(length).Length);
    }

    [Fact]
    public void AnAliasOfLengthMustHaveRoomForTheSuffix()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Alias().OfLength(7));
    }
}
