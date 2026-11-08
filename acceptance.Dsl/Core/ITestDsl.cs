namespace acceptance.Core.Dsl;

// What every domain's test DSL offers, whichever domain the last action was
// in. Each I<Domain>TestDsl extends it.
public interface ITestDsl
{
    Task ConfirmErrorIsShown(string message);
}
