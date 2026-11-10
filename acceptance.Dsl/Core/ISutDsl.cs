namespace acceptance.Core.Dsl;

public interface ISutDsl
{
    Task ConfirmErrorIsShown(string message);
}
