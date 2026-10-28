using System.Reflection;
using Xunit.v3;

namespace acceptance.Infrastructure;

// Limits a spec to the channels it applies to: [Fact, Channels(Channel.Ui)].
// Specs without it run on every channel, which should stay the default.
// On other channels the spec is reported as skipped, never silently dropped.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ChannelsAttribute(params Channel[] channels) : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        var current = AcceptanceChannel.Current;
        if (!channels.Contains(current))
            Assert.Skip($"Not applicable to the {current} channel");
    }
}
