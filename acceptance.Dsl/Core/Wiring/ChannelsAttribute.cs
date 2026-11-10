using System.Reflection;
using Xunit.v3;

namespace acceptance.Core.Dsl;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ChannelsAttribute(params ChannelType[] channels) : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        var current = AcceptanceChannel.Current;
        if (!channels.Contains(current))
            Assert.Skip($"Not applicable to the {current} channel");
    }
}
