namespace AIforMAsupport.Api.Services.Ai;

public interface IAiClient
{
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default);

    // Same call, but onTextDelta is invoked with each piece of the answer as the model produces
    // it (so the UI can show the answer growing instead of waiting for all of it). Returns the
    // complete final answer text, identical to what AskAsync would have returned.
    Task<string> AskStreamAsync(string prompt, Func<string, Task> onTextDelta, CancellationToken cancellationToken = default);
}
