namespace acceptance.Core.Dsl.UnitTests;

public class VocabularyTests
{
    private enum Priority { Low, Normal, High }

    private static readonly Vocabulary<Priority> Priorities = new("priority")
    {
        ["low"] = Priority.Low,
        ["normal"] = Priority.Normal,
        ["high"] = Priority.High,
    };

    [Fact]
    public void AKnownWordIsTranslated()
    {
        Assert.Equal(Priority.High, Priorities.Translate("high"));
    }

    [Fact]
    public void AnUnknownWordIsAMistakeInTheSpec()
    {
        var mistake = Assert.Throws<InvalidSpecException>(() => Priorities.Translate("hihg"));

        Assert.Equal(
            "The spec is wrong: 'hihg' is not a recognised priority. Use one of: low, normal, high.",
            mistake.Message);
    }

    [Fact]
    public void WordsMustMatchExactly()
    {
        Assert.Throws<InvalidSpecException>(() => Priorities.Translate("High"));
    }

    [Fact]
    public void AWordCanOnlyMeanOneThing()
    {
        Assert.Throws<ArgumentException>(() => new Vocabulary<Priority>("priority")
        {
            ["low"] = Priority.Low,
            ["low"] = Priority.Normal,
        });
    }
}
