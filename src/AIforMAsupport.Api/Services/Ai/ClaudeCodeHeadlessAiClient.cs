using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.Ai;

// Calls the Claude Code CLI in headless mode using the user's existing subscription login
// (no separate Anthropic API key). See CLAUDE.md for the exact invocation this mirrors.
// --restricted disables Bash/Edit/etc so the chatbot only ever gets read-only answering ability.
public sealed class ClaudeCodeHeadlessAiClient : IAiClient
{
    private readonly ClaudeCliOptions _options;
    private readonly ILogger<ClaudeCodeHeadlessAiClient> _logger;
    private readonly string _sandboxWorkingDirectory;

    public ClaudeCodeHeadlessAiClient(IOptions<ClaudeCliOptions> options, ILogger<ClaudeCodeHeadlessAiClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // If left as the API's own CWD (inside this repo), claude auto-discovers and loads this
        // repo's own CLAUDE.md (meant for a coding assistant working ON this project) alongside
        // the MA Copilot system prompt - internal details (class names, file paths, the
        // --restricted flag itself) can then leak into a real support answer once the model has
        // nothing from the KB context to anchor on. An empty directory outside the repo has
        // no CLAUDE.md anywhere in its parent chain, so there's nothing to discover.
        _sandboxWorkingDirectory = Path.Combine(Path.GetTempPath(), "ma-case-copilot-ai-sandbox");
        Directory.CreateDirectory(_sandboxWorkingDirectory);
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var startInfo = BuildStartInfo(stream: false);

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        process.Start();

        // Write stdin and read stdout/stderr concurrently - see the comment on WriteStdinAsync
        // for why awaiting the write to finish before starting the reads (the previous code here)
        // is a real deadlock risk once the prompt grows past the OS pipe buffer.
        var stdinTask = WriteStdinAsync(process, prompt);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(linkedCts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException($"claude CLI did not respond within {_options.TimeoutSeconds}s");
        }
        catch (OperationCanceledException)
        {
            // The caller cancelled (the user pressed Stop, or the browser went away): kill the
            // CLI so it stops consuming subscription quota, then let the cancellation propagate.
            TryKill(process);
            throw;
        }

        await stdinTask;
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        // Even on a non-zero exit code (e.g. not logged in, API error), the CLI still emits a
        // JSON body with a "result" field describing what went wrong - that message is far more
        // useful than the (often empty) stderr stream, so prefer it whenever stdout parses.
        if (TryExtractResult(stdout, out var result, out var isError))
        {
            if (isError)
            {
                _logger.LogError("claude CLI reported an error. result: {Result}", result);
                throw new InvalidOperationException($"claude CLI reported an error: {result}");
            }

            return result;
        }

        _logger.LogError("claude CLI exited with code {ExitCode}. stderr: {Stderr}", process.ExitCode, stderr);
        throw new InvalidOperationException($"claude CLI failed (exit {process.ExitCode}): {stderr}");
    }

    // Streaming variant: the CLI's "stream-json" output is one JSON object per line. The pieces
    // of the answer arrive as {"type":"stream_event","event":{"type":"content_block_delta",
    // "delta":{"type":"text_delta","text":"..."}}} lines, and the very last line is the same
    // {"type":"result",...} object the plain "json" format emits, which stays the source of truth
    // for the final text and the error flag.
    public async Task<string> AskStreamAsync(string prompt, Func<string, Task> onTextDelta, CancellationToken cancellationToken = default)
    {
        var startInfo = BuildStartInfo(stream: true);

        using var process = new Process { StartInfo = startInfo };
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        process.Start();

        // See the comment on WriteStdinAsync - must run concurrently with the stdout read loop
        // below, not be awaited to completion first, or a large prompt can deadlock the pipes.
        var stdinTask = WriteStdinAsync(process, prompt);
        var stderrTask = process.StandardError.ReadToEndAsync(linkedCts.Token);
        var streamed = new StringBuilder();
        string? finalResult = null;
        var finalIsError = false;
        var answerComplete = false;

        try
        {
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync(linkedCts.Token)) is not null)
            {
                if (line.Length == 0)
                {
                    continue;
                }

                if (TryParseStreamLine(line, out var delta, out var result, out var isError, out var messageStopped))
                {
                    if (delta is not null)
                    {
                        streamed.Append(delta);
                        await onTextDelta(delta);
                    }
                    else if (result is not null)
                    {
                        finalResult = result;
                        finalIsError = isError;
                    }
                    else if (messageStopped && streamed.Length > 0)
                    {
                        // The answer is complete. What the CLI still does after this (writing a
                        // short post-turn summary, then the final result line) adds a few seconds
                        // of waiting for nothing we use - the streamed text IS the answer - so stop
                        // here and let the process go. An error never gets this far: it arrives as
                        // a result line with no text streamed before it.
                        finalResult = streamed.ToString();
                        answerComplete = true;
                        break;
                    }
                }
            }

            if (answerComplete)
            {
                TryKill(process);
            }
            else
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException($"claude CLI did not respond within {_options.TimeoutSeconds}s");
        }
        catch (OperationCanceledException)
        {
            // Stop pressed / browser gone: kill the CLI so it stops using subscription quota.
            TryKill(process);
            throw;
        }
        catch
        {
            // e.g. the consumer failed while forwarding a delta - don't leave the CLI running.
            TryKill(process);
            throw;
        }

        await stdinTask;
        var stderr = await stderrTask;

        if (finalResult is not null)
        {
            if (finalIsError)
            {
                _logger.LogError("claude CLI reported an error. result: {Result}", finalResult);
                throw new InvalidOperationException($"claude CLI reported an error: {finalResult}");
            }

            return finalResult;
        }

        _logger.LogError("claude CLI stream ended without a result (exit {ExitCode}). stderr: {Stderr}", process.ExitCode, stderr);
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(stderr) ? $"claude CLI failed (exit {process.ExitCode})" : $"claude CLI failed (exit {process.ExitCode}): {stderr}");
    }

    private ProcessStartInfo BuildStartInfo(bool stream)
    {
        var systemPromptPath = Path.GetFullPath(_options.SystemPromptFile);
        if (!File.Exists(systemPromptPath))
        {
            throw new FileNotFoundException($"System prompt file not found: {systemPromptPath}", systemPromptPath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveExecutablePath(_options.ExecutablePath),
            WorkingDirectory = _sandboxWorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Without this, redirected stdin/stdout fall back to the system ANSI codepage on
            // Windows (e.g. cp874/cp1252), which mangles Thai text into '?' before it ever
            // reaches the claude CLI.
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add("--output-format");
        if (stream)
        {
            // stream-json requires --verbose; --include-partial-messages is what makes the text
            // arrive in small deltas instead of one finished message at the end.
            startInfo.ArgumentList.Add("stream-json");
            startInfo.ArgumentList.Add("--verbose");
            startInfo.ArgumentList.Add("--include-partial-messages");
        }
        else
        {
            startInfo.ArgumentList.Add("json");
        }

        startInfo.ArgumentList.Add("--effort");
        startInfo.ArgumentList.Add(_options.EffortLevel);
        startInfo.ArgumentList.Add("--restricted");
        // --restricted alone still loads MCP servers from the user's own global/account config
        // (this machine had Google Drive, Zapier, etc. connected) - a support chatbot answering
        // Bridgestone MA questions has no business touching any of that. With no --mcp-config
        // given, --strict-mcp-config means zero MCP servers load at all.
        startInfo.ArgumentList.Add("--strict-mcp-config");
        // All KB searching happens server-side (KeywordSearchKbContextProvider) before the
        // prompt is even built - the model only ever needs to reason over the
        // "[เคสอ้างอิงที่เกี่ยวข้อง]" text already in the prompt. Without this, when that section
        // comes back empty the model tries to use Read/Grep to go "search" for the KB itself
        // instead of just following the system prompt's "KB ไม่มีข้อมูลเรื่องนี้" rule.
        startInfo.ArgumentList.Add("--disallowedTools");
        startInfo.ArgumentList.Add("Read,Grep,Glob,WebSearch,Task");
        startInfo.ArgumentList.Add("--system-prompt-file");
        startInfo.ArgumentList.Add(systemPromptPath);

        return startInfo;
    }

    // Process.Start(UseShellExecute=false) does not do the PATHEXT-style resolution a shell
    // does, so an extensionless "claude" never finds the npm-installed claude.cmd shim on
    // Windows. If the caller didn't already specify an extension or a path, fall back to
    // claude.cmd on Windows.
    //
    // Prefer the native claude.exe next to that shim when one exists (npm's claude-code package
    // ships one - the .cmd is just "@ECHO off ... "%dp0%\node_modules\@anthropic-ai\claude-code\
    // bin\claude.exe" %*"), instead of running the .cmd itself. Reason: a .cmd target makes .NET's
    // Process class launch it via an implicit "cmd.exe /c", and that extra cmd.exe hop was
    // observed to mangle the UTF-8 prompt into literal '?' characters before it reached the real
    // claude process (StandardInputEncoding=UTF8 below only governs the pipe from this .NET
    // process to its immediate child, i.e. to cmd.exe - not what cmd.exe then does when it
    // re-forwards that input to claude.exe, which follows its own console codepage instead).
    // Invoking claude.exe directly removes that hop entirely, so the same UTF8 setting reaches
    // the process that actually needs it. Thai/English input piped through a plain PowerShell
    // "|" into claude.cmd was verified to render fine on its own (see session notes) - so this is
    // specific to .NET's cmd.exe-wrapped Process.Start, not the CLI or the terminal in general.
    private static string ResolveExecutablePath(string executablePath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return executablePath;
        }

        var hasPathSeparator = executablePath.Contains(Path.DirectorySeparatorChar)
            || executablePath.Contains(Path.AltDirectorySeparatorChar);
        var hasExtension = !string.IsNullOrEmpty(Path.GetExtension(executablePath));

        if (hasPathSeparator || hasExtension)
        {
            return executablePath;
        }

        var cmdPath = FindOnPath(executablePath + ".cmd");
        if (cmdPath is not null)
        {
            var nativeExe = Path.Combine(Path.GetDirectoryName(cmdPath)!, "node_modules", "@anthropic-ai", "claude-code", "bin", "claude.exe");
            if (File.Exists(nativeExe))
            {
                return nativeExe;
            }
        }

        // Older/differently-installed CLI without a bundled native exe: same behavior as before
        // (still works, just carries the encoding risk above for non-ASCII prompts).
        return executablePath + ".cmd";
    }

    private static string? FindOnPath(string fileName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(dir.Trim('"'), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool TryExtractResult(string stdout, out string result, out bool isError)
    {
        result = string.Empty;
        isError = false;

        try
        {
            using var doc = JsonDocument.Parse(stdout);
            if (!doc.RootElement.TryGetProperty("result", out var resultProp))
            {
                return false;
            }

            result = resultProp.GetString() ?? string.Empty;
            isError = doc.RootElement.TryGetProperty("is_error", out var isErrorProp) && isErrorProp.GetBoolean();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // One line of stream-json output. Yields either a text delta, or the final result (with its
    // error flag); every other line type (init, status, usage, rate limit, ...) is ignored.
    private static bool TryParseStreamLine(string line, out string? delta, out string? result, out bool isError, out bool messageStopped)
    {
        delta = null;
        result = null;
        isError = false;
        messageStopped = false;

        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp))
            {
                return false;
            }

            switch (typeProp.GetString())
            {
                case "stream_event":
                    if (root.TryGetProperty("event", out var stopEv)
                        && stopEv.TryGetProperty("type", out var stopType) && stopType.GetString() == "message_stop")
                    {
                        messageStopped = true;
                        return true;
                    }

                    if (root.TryGetProperty("event", out var ev)
                        && ev.TryGetProperty("type", out var evType) && evType.GetString() == "content_block_delta"
                        && ev.TryGetProperty("delta", out var d)
                        && d.TryGetProperty("type", out var dType) && dType.GetString() == "text_delta"
                        && d.TryGetProperty("text", out var textProp))
                    {
                        delta = textProp.GetString();
                        return delta is not null;
                    }

                    return false;

                case "result":
                    if (root.TryGetProperty("result", out var resultProp))
                    {
                        result = resultProp.GetString() ?? string.Empty;
                        isError = root.TryGetProperty("is_error", out var isErrorProp) && isErrorProp.ValueKind == JsonValueKind.True;
                        return true;
                    }

                    return false;

                default:
                    return false;
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // Writes the whole prompt to the process's stdin and closes it. Must be started concurrently
    // with reading stdout/stderr (see AskAsync/AskStreamAsync), never awaited to completion
    // before those reads begin - that was the previous code here, and it deadlocks once the
    // prompt is large enough: this app's prompts now routinely carry several turns of
    // conversation history, full column definitions for several DB tables, and Known Script SQL
    // text, easily tens of thousands of characters. .NET's redirected stdin pipe has a limited OS
    // buffer, so writing that much blocks until the child process reads enough to make room - and
    // with --verbose (the streaming path), the child can itself start writing status/output to
    // stdout before it has finished consuming stdin. If nothing is draining stdout yet because
    // the parent is still stuck inside this write, both pipes fill up and each side waits on the
    // other forever: no exception, no timeout (the write has no cancellation token to trip), the
    // request just hangs - this is exactly the "answers then hangs" bug a large "Send this result
    // to AI" payload triggered in testing. Swallows write failures on purpose: if the process
    // already exited (crashed, or got killed for an unrelated timeout/cancellation), writing to
    // its closed stdin throws too, but that's a symptom, not the real failure - the caller's own
    // stdout/stderr/exit-code handling already reports the actual cause.
    private static async Task WriteStdinAsync(Process process, string prompt)
    {
        try
        {
            await process.StandardInput.WriteAsync(prompt);
            process.StandardInput.Close();
        }
        catch
        {
            // best-effort - see comment above
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
