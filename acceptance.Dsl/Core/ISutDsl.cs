namespace acceptance.Core.Dsl;

// What every domain's DSL offers on the channel under test (Sut.), whichever
// domain the last action was in. Each I<Domain>SutDsl extends it.
public interface ISutDsl
{
    Task ConfirmErrorIsShown(string message);
}
