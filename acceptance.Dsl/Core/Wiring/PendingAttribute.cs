using System.Reflection;
using Xunit.v3;

namespace acceptance.Core.Dsl;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PendingAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test) =>
        Assert.Skip("Pending: written but not started. Remove [Pending] to start it");
}
