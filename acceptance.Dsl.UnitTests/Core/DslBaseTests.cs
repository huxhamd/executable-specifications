using acceptance.Core.Drivers;
using Xunit.Sdk;

namespace acceptance.Core.Dsl.UnitTests;

public class DslBaseTests
{
    private readonly SpecContext _context = new();

    private TestDsl Sut => new(_context);
    private TestDsl Api => new(_context.ForPinnedChannel("Api"));

    [Fact]
    public void ASuccessfulActionIsConfirmed()
    {
        var dsl = Sut;
        dsl.RecordResult(DriverResult.Okay);

        dsl.ConfirmSucceeded();
    }

    [Fact]
    public void AFailedActionOnTheChannelUnderTestFailsTheSpec()
    {
        var dsl = Sut;
        dsl.RecordResult(DriverResult.Fail("A title is required"));

        var failure = Assert.ThrowsAny<Exception>(dsl.ConfirmSucceeded);

        Assert.IsAssignableFrom<IAssertionException>(failure);
        Assert.Contains("The system said: A title is required", failure.Message);
    }

    [Fact]
    public void AFailedActionOnThePinnedChannelIsAnArrangementFailure()
    {
        var dsl = Api;
        dsl.RecordResult(DriverResult.Fail("A title is required"));

        var failure = Assert.Throws<ArrangementFailedException>(dsl.ConfirmSucceeded);

        Assert.Contains("through Api", failure.Message);
        Assert.Contains("The system said: A title is required", failure.Message);
    }

    [Fact]
    public void ConfirmingWithNoActionRecordedIsAHarnessError()
    {
        Assert.Throws<InvalidOperationException>(Sut.ConfirmSucceeded);
    }

    [Fact]
    public void ACapabilityTheChannelHasIsReturned()
    {
        var capability = new object();

        Assert.Same(capability, Sut.RequireCapability(capability));
    }

    [Fact]
    public void ACapabilityTheChannelUnderTestLacksSuggestsLimitingTheSpecsChannels()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Sut.RequireCapability(null));

        Assert.Contains("live feedback", failure.Message);
        Assert.Contains("[Fact, Channels(", failure.Message);
    }

    [Fact]
    public void ACapabilityThePinnedChannelLacksSuggestsAddingItToItsDrivers()
    {
        var failure = Assert.Throws<InvalidOperationException>(() => Api.RequireCapability(null));

        Assert.Contains("live feedback through Api", failure.Message);
        Assert.Contains("drivers", failure.Message);
    }

    [Fact]
    public async Task AnErrorIsConfirmedWhenTheLastActionFailedWithIt()
    {
        var dsl = Sut;
        dsl.RecordResult(DriverResult.Fail("A title is required"));

        await dsl.ConfirmErrorIsShown("A title is required");
    }

    [Fact]
    public async Task AnErrorIsNotConfirmedWhenTheLastActionSucceeded()
    {
        var dsl = Sut;
        dsl.RecordResult(DriverResult.Okay);

        var failure = await Assert.ThrowsAnyAsync<Exception>(() => dsl.ConfirmErrorIsShown("A title is required"));

        Assert.IsAssignableFrom<IAssertionException>(failure);
        Assert.Contains("Expected the last action to fail, but it succeeded.", failure.Message);
    }

    [Fact]
    public async Task AnErrorIsNotConfirmedWhenTheSystemSaidSomethingElse()
    {
        var dsl = Sut;
        dsl.RecordResult(DriverResult.Fail("The title is too long"));

        var failure = await Assert.ThrowsAnyAsync<Exception>(() => dsl.ConfirmErrorIsShown("A title is required"));

        Assert.IsAssignableFrom<IAssertionException>(failure);
    }

    private sealed class TestDsl(SpecContext context) : DslBase(context)
    {
        public void RecordResult(DriverResult result) => Record(result);

        public void ConfirmSucceeded() => ConfirmLastActionSucceeded("Could not add todo 'Buy milk'");

        public object RequireCapability(object? capability) => Require(capability, "live feedback");
    }
}
