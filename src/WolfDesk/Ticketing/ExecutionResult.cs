namespace dev.kaldiroglu.WolfDesk.Ticketing;

/// <summary>What an application service operation reports back: success, or failure with its reason.</summary>
public abstract record ExecutionResult
{
    public sealed record Success : ExecutionResult;

    public sealed record Failure(string Reason) : ExecutionResult;

    public static ExecutionResult Succeeded() => new Success();

    public static ExecutionResult Failed(Exception e) => new Failure(e.Message);
}
