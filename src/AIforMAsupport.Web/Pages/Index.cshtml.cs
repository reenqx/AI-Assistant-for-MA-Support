using AIforMAsupport.Web.Services.Api;
using AIforMAsupport.Web.Services.Markdown;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIforMAsupport.Web.Pages;

// Plain request/response JSON handlers (no antiforgery token needed - there's no login/session
// cookie here to protect against CSRF, this is an internal read-only chat tool). Chosen over
// Blazor Server's SignalR circuit because each chat turn really is a one-shot request/response:
// no server push, no shared multi-user state, so the extra moving parts of a persistent circuit
// only added failure modes (see git history) without buying anything this UI needs.
[IgnoreAntiforgeryToken]
public sealed class IndexModel : PageModel
{
    private readonly ICaseCopilotApiClient _apiClient;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ICaseCopilotApiClient apiClient, ILogger<IndexModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public sealed record ChatRequest(string Question, Guid? ConversationId);

    // Full case content now flows to the client (KbContent included) - the split-pane console's
    // Reference Case Drawer needs it for an instant peek without a second round trip. The old
    // chat-box UI this replaced deliberately stripped KbContent from CasePreview; that's no
    // longer the right call once there's a drawer built to show it.
    public sealed record CasePreview(
        string Id,
        string BsModule,
        string SubCategory,
        string CaseType,
        string Summary,
        string Description,
        string StepsToReproduce,
        string AdditionalInformation,
        string Notes,
        string KbContent);

    public sealed record KnownScriptPreview(string Name, string Description, string SqlContent);

    // Exactly one of ScriptName/SqlText should be set - see ICaseCopilotApiClient.RunSqlAsync.
    public sealed record SqlRunRequest(string? ScriptName, string? SqlText, Dictionary<string, string> Parameters);

    public sealed record SqlTablePreview(string? Label, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string?>> Rows);

    public sealed record SqlRunResponse(bool Success, string? ErrorMessage, IReadOnlyList<SqlTablePreview> Tables);

    // Fast path: KB search only (typically <1s), so the page can show candidate cases before
    // the slow AI call even starts. IsFinal true means there's nothing more to fetch (a raw
    // "#<id>" lookup, a not-found, or a friendly error) - the client should skip calling
    // OnPostAskAsync entirely in that case and just render FinalAnswerHtml.
    public sealed record SearchResponse(
        Guid ConversationId,
        IReadOnlyList<CasePreview> Cases,
        bool IsFinal,
        string? FinalAnswerHtml,
        IReadOnlyList<KnownScriptPreview> RelevantScripts);

    public sealed record AskResponse(
        Guid ConversationId,
        string AnswerHtml,
        IReadOnlyList<string> ReferencedCaseIds,
        bool IsRawCaseLookup,
        IReadOnlyList<KnownScriptPreview> RelevantScripts,
        // Raw text (before markdown->HTML rendering) - only used client-side to pre-fill the
        // "บันทึกเคส" save dialog's textarea (see console.js) with clean plain text instead of
        // having to strip tags back out of AnswerHtml.
        string Answer);

    private static CasePreview ToPreview(KbCaseSummary c) => new(
        c.Id, c.BsModule, c.SubCategory, c.CaseType, c.Summary,
        c.Description, c.StepsToReproduce, c.AdditionalInformation, c.Notes, c.KbContent);

    private static KnownScriptPreview ToPreview(KnownScriptDto s) => new(s.Name, s.Description, s.SqlContent);

    // Renders the answer through the same safe HTML-escaping SimpleMarkdown pipeline as before,
    // then tags any plain code block that's a real (whitelisted, read-only) Known Script with a
    // "รันจริง" button wired to the same live-execution endpoint SQL Studio uses, then applies a
    // step/branch re-styling pass (a no-op for any answer that doesn't use "ขั้น N" / "เจอ →" /
    // "ไม่เจอ →" phrasing), then turns every "#1234" case reference into a clickable pill wired to
    // the Reference Case Drawer.
    private static string RenderAnswer(string text, IReadOnlyList<KnownScriptPreview> scripts) =>
        CaseReferenceFormatter.Enhance(
            DecisionTreeFormatter.Enhance(
                RunnableSqlFormatter.Enhance(SimpleMarkdown.ToHtml(text), scripts)));

    public async Task<IActionResult> OnPostSearchAsync([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest();
        }

        try
        {
            var response = await _apiClient.SearchAsync(request.Question, request.ConversationId, cancellationToken);
            var previews = response.Cases.Select(ToPreview).ToList();
            var scripts = response.RelevantScripts.Select(ToPreview).ToList();
            var finalAnswerHtml = response.IsFinal ? RenderAnswer(response.FinalAnswer ?? string.Empty, scripts) : null;

            return new JsonResult(new SearchResponse(response.ConversationId, previews, response.IsFinal, finalAnswerHtml, scripts));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Search request cancelled by the client.");
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search the AI Assistant for MA Support API.");
            var errorHtml = SimpleMarkdown.ToHtml($"เกิดข้อผิดพลาดในการเชื่อมต่อ API: {ex.Message}");
            return new JsonResult(new SearchResponse(request.ConversationId ?? Guid.NewGuid(), [], true, errorHtml, []));
        }
    }

    public async Task<IActionResult> OnPostAskAsync([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest();
        }

        try
        {
            var response = await _apiClient.AskAsync(request.Question, request.ConversationId, cancellationToken);
            var scripts = response.RelevantScripts.Select(ToPreview).ToList();
            var answerHtml = RenderAnswer(response.Answer, scripts);
            return new JsonResult(new AskResponse(response.ConversationId, answerHtml, response.ReferencedCaseIds, response.IsRawCaseLookup, scripts, response.Answer));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ask request cancelled by the client.");
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get an answer from the AI Assistant for MA Support API.");
            var errorText = $"เกิดข้อผิดพลาดในการเชื่อมต่อ API: {ex.Message}";
            var errorHtml = SimpleMarkdown.ToHtml(errorText);
            return new JsonResult(new AskResponse(request.ConversationId ?? Guid.NewGuid(), errorHtml, [], false, [], errorText));
        }
    }

    // Live check for the SQL editor: what would running this text be (read-only / modifies rows /
    // blocked outright) and does it still have values to fill in? Uses the same shared rule the API
    // enforces, so the editor's notice never disagrees with what the API will do - but the API
    // re-checks everything itself when the SQL is actually run; this only informs the person typing.
    public sealed record ClassifySqlRequest(string? SqlText);

    public sealed record ClassifySqlResponse(string Risk, string? Reason, bool Unfilled);

    public IActionResult OnPostClassifySql([FromBody] ClassifySqlRequest request)
    {
        var sql = request?.SqlText ?? string.Empty;
        var classification = AIforMAsupport.Shared.SqlSafetyClassifier.ClassifyDisplayed(sql);
        var risk = classification.Risk switch
        {
            AIforMAsupport.Shared.SqlRisk.Blocked => "blocked",
            AIforMAsupport.Shared.SqlRisk.Modifies => "modifies",
            _ => "readOnly",
        };
        return new JsonResult(new ClassifySqlResponse(risk, classification.Reason, SqlPlaceholderCheck.HasUnfilled(sql)));
    }

    // Streaming variant of OnPostAskAsync: the answer is sent to the browser while it is still being
    // written. The response is newline-delimited JSON -
    //   {"type":"delta","html":"..."}    the answer so far, rendered to HTML (sent at most every
    //                                    PartialInterval so a fast model doesn't flood the browser)
    //   {"type":"done","response":{...}} the final AskResponse, exactly what OnPostAskAsync returns
    //                                    (an error becomes a normal AskResponse holding the error
    //                                    text, same as OnPostAskAsync)
    // The partial HTML deliberately skips RunnableSqlFormatter (no "Run live" buttons on half an
    // answer); the final one is fully rendered.
    private static readonly TimeSpan PartialInterval = TimeSpan.FromMilliseconds(120);

    private static readonly System.Text.Json.JsonSerializerOptions StreamJson = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string RenderPartialAnswer(string text) =>
        CaseReferenceFormatter.Enhance(DecisionTreeFormatter.Enhance(SimpleMarkdown.ToHtml(text)));

    public async Task<IActionResult> OnPostAskStreamAsync([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest();
        }

        Response.ContentType = "application/x-ndjson";
        Response.Headers.CacheControl = "no-cache";
        HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

        async Task WriteLineAsync(object payload)
        {
            await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(payload, StreamJson) + "\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        var text = new System.Text.StringBuilder();
        long lastSentAt = 0;

        try
        {
            var response = await _apiClient.AskStreamAsync(
                request.Question,
                request.ConversationId,
                async delta =>
                {
                    text.Append(delta);
                    var now = System.Diagnostics.Stopwatch.GetTimestamp();
                    if (lastSentAt == 0 || System.Diagnostics.Stopwatch.GetElapsedTime(lastSentAt, now) >= PartialInterval)
                    {
                        lastSentAt = now;
                        await WriteLineAsync(new { type = "delta", html = RenderPartialAnswer(text.ToString()) });
                    }
                },
                cancellationToken);

            var scripts = response.RelevantScripts.Select(ToPreview).ToList();
            var answerHtml = RenderAnswer(response.Answer, scripts);
            await WriteLineAsync(new
            {
                type = "done",
                response = new AskResponse(response.ConversationId, answerHtml, response.ReferencedCaseIds, response.IsRawCaseLookup, scripts, response.Answer),
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ask stream cancelled by the client.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get a streamed answer from the AI Assistant for MA Support API.");
            var errorText = $"เกิดข้อผิดพลาดในการเชื่อมต่อ API: {ex.Message}";
            var errorHtml = SimpleMarkdown.ToHtml(errorText);
            try
            {
                await WriteLineAsync(new
                {
                    type = "done",
                    response = new AskResponse(request.ConversationId ?? Guid.NewGuid(), errorHtml, [], false, [], errorText),
                });
            }
            catch
            {
                // The browser is already gone - nothing left to tell.
            }
        }

        return new EmptyResult();
    }

    // Live, read-only execution against the real database - either a whitelisted KnownScript by
    // name, or raw AI-authored SQL text (the user explicitly chose to allow this too - still
    // read-only only, see SqlScriptRunner). Always triggered by a human click - see CLAUDE.md's
    // SQL Studio scope entry. Errors surfaced by the API (feature disabled, write statement, DB
    // unreachable) are returned as a normal 200 JsonResult with Success=false, matching how
    // Search/Ask already avoid throwing HTTP error statuses back to the client-side fetch() calls.
    public async Task<IActionResult> OnPostRunSqlAsync([FromBody] SqlRunRequest request, CancellationToken cancellationToken)
    {
        if (request is null || (string.IsNullOrWhiteSpace(request.ScriptName) && string.IsNullOrWhiteSpace(request.SqlText)))
        {
            return BadRequest();
        }

        try
        {
            var result = await _apiClient.RunSqlAsync(request.ScriptName, request.SqlText, request.Parameters ?? new Dictionary<string, string>(), cancellationToken);
            if (!result.Success || result.Response is null)
            {
                return new JsonResult(new SqlRunResponse(false, result.ErrorMessage ?? "รันสคริปต์ไม่สำเร็จ", []));
            }

            var tables = result.Response.Tables.Select(t => new SqlTablePreview(t.Label, t.Columns, t.Rows)).ToList();
            return new JsonResult(new SqlRunResponse(true, null, tables));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run a live SQL script via the AI Assistant for MA Support API.");
            return new JsonResult(new SqlRunResponse(false, $"เกิดข้อผิดพลาดในการเชื่อมต่อ API: {ex.Message}", []));
        }
    }

    // The person reviewed (and possibly edited) the AI's "บันทึกเคส" summary in the save dialog
    // before this fires - see console.js's save-case flow. Summary is saved exactly as sent,
    // never re-derived from ConversationId, so an edit sticks.
    public sealed record SaveCaseRequest(
        Guid ConversationId,
        string Summary,
        IReadOnlyList<string>? ReferencedCaseIds,
        IReadOnlyList<string>? KnownScripts);

    public sealed record SavedCasePreview(
        int Id,
        Guid ConversationId,
        string Summary,
        IReadOnlyList<string> ReferencedCaseIds,
        IReadOnlyList<string> KnownScripts,
        DateTime SavedAtUtc,
        string? MantisIssueId);

    public async Task<IActionResult> OnPostSaveCaseAsync([FromBody] SaveCaseRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Summary))
        {
            return BadRequest();
        }

        try
        {
            var saved = await _apiClient.SaveCaseAsync(
                request.ConversationId,
                request.Summary,
                request.ReferencedCaseIds ?? [],
                request.KnownScripts ?? [],
                cancellationToken);

            return new JsonResult(new SavedCasePreview(
                saved.Id, saved.ConversationId, saved.Summary, saved.ReferencedCaseIds, saved.KnownScripts, saved.SavedAtUtc, saved.MantisIssueId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save a case via the AI Assistant for MA Support API.");
            return StatusCode(502, new { error = $"บันทึกเคสไม่สำเร็จ: {ex.Message}" });
        }
    }
}
