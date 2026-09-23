using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.Ai;
using AIforMAsupport.Api.Services.Chat;
using AIforMAsupport.Api.Services.Conversation;
using AIforMAsupport.Api.Services.History;
using AIforMAsupport.Api.Services.KnowledgeBase;
using AIforMAsupport.Api.Services.KnownScripts;
using AIforMAsupport.Api.Services.Sql;
using Microsoft.AspNetCore.Mvc;

namespace AIforMAsupport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly IAiClient _aiClient;
    private readonly IKbContextProvider _kbContextProvider;
    private readonly IKbCaseRepository _kbCaseRepository;
    private readonly IKnownScriptRepository _knownScriptRepository;
    private readonly IDbSchemaProvider _dbSchemaProvider;
    private readonly IConversationStore _conversationStore;
    private readonly IConversationHistoryStore _historyStore;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAiClient aiClient,
        IKbContextProvider kbContextProvider,
        IKbCaseRepository kbCaseRepository,
        IKnownScriptRepository knownScriptRepository,
        IDbSchemaProvider dbSchemaProvider,
        IConversationStore conversationStore,
        IConversationHistoryStore historyStore,
        ILogger<ChatController> logger)
    {
        _aiClient = aiClient;
        _kbContextProvider = kbContextProvider;
        _kbCaseRepository = kbCaseRepository;
        _knownScriptRepository = knownScriptRepository;
        _dbSchemaProvider = dbSchemaProvider;
        _conversationStore = conversationStore;
        _historyStore = historyStore;
        _logger = logger;
    }

    // Fast path: command parsing + KB search only, no AI call. Lets the UI show candidate cases
    // right away instead of a spinner for however long the headless claude CLI call takes.
    [HttpPost("search")]
    public async Task<ActionResult<ChatSearchResponse>> Search([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required.");
        }

        var conversationId = request.ConversationId ?? Guid.NewGuid();
        var resolved = await Resolve(conversationId, request.Question, cancellationToken);

        if (resolved.IsFinal)
        {
            var questionForStorage = resolved.QuestionForStorage ?? request.Question;
            var referencedCaseIds = resolved.Context.Select(c => c.Id).ToList();

            _conversationStore.SetLastTurn(conversationId, new ConversationTurn(questionForStorage, resolved.Context, resolved.FinalAnswer!));
            await _historyStore.AppendAsync(conversationId, questionForStorage, resolved.FinalAnswer!, referencedCaseIds, cancellationToken);

            _logger.LogInformation(
                "Chat search resolved final. ConversationId={ConversationId} ReferencedCaseIds={ReferencedCaseIds}",
                conversationId,
                referencedCaseIds);
        }

        return Ok(new ChatSearchResponse(conversationId, resolved.Context, resolved.IsFinal, resolved.FinalAnswer, resolved.RelevantScripts));
    }

    // Slow path: calls the AI to synthesize a full answer over whatever Resolve() (the same
    // logic Search() uses) determines the relevant context to be. Safe to call on its own
    // without calling Search() first - it re-derives the same context, since KB search is a
    // deterministic, effectively free (<1s, in-memory) operation.
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required.");
        }

        var conversationId = request.ConversationId ?? Guid.NewGuid();
        var resolved = await Resolve(conversationId, request.Question, cancellationToken);

        if (resolved.IsFinal)
        {
            var questionForStorage = resolved.QuestionForStorage ?? request.Question;
            var finalReferencedCaseIds = resolved.Context.Select(c => c.Id).ToList();

            _conversationStore.SetLastTurn(conversationId, new ConversationTurn(questionForStorage, resolved.Context, resolved.FinalAnswer!));
            await _historyStore.AppendAsync(conversationId, questionForStorage, resolved.FinalAnswer!, finalReferencedCaseIds, cancellationToken);

            return Ok(new ChatResponse(conversationId, resolved.FinalAnswer!, finalReferencedCaseIds, true, resolved.RelevantScripts));
        }

        var answer = await _aiClient.AskAsync(resolved.Prompt!, cancellationToken);
        return Ok(await CompleteAiTurnAsync(conversationId, request.Question, resolved, answer, cancellationToken));
    }

    // Same as Ask, but the answer is sent to the caller as it is being written: the response is
    // newline-delimited JSON, one object per line -
    //   {"type":"delta","text":"..."}        a piece of the answer as the model produces it
    //   {"type":"done","response":{...}}     the final ChatResponse (same shape Ask returns)
    //   {"type":"error","message":"..."}     the AI call failed
    // Nothing is stored unless the answer completes, exactly like Ask: a stopped/aborted request
    // (the caller disconnects) cancels the token, which also kills the claude process.
    [HttpPost("stream")]
    public async Task Stream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        Response.ContentType = "application/x-ndjson";
        Response.Headers.CacheControl = "no-cache";
        HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>()?.DisableBuffering();

        async Task WriteLineAsync(object payload)
        {
            await Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(payload, StreamJson) + "\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        var conversationId = request.ConversationId ?? Guid.NewGuid();

        try
        {
            var resolved = await Resolve(conversationId, request.Question, cancellationToken);

            if (resolved.IsFinal)
            {
                var questionForStorage = resolved.QuestionForStorage ?? request.Question;
                var finalReferencedCaseIds = resolved.Context.Select(c => c.Id).ToList();

                _conversationStore.SetLastTurn(conversationId, new ConversationTurn(questionForStorage, resolved.Context, resolved.FinalAnswer!));
                await _historyStore.AppendAsync(conversationId, questionForStorage, resolved.FinalAnswer!, finalReferencedCaseIds, cancellationToken);

                await WriteLineAsync(new { type = "done", response = new ChatResponse(conversationId, resolved.FinalAnswer!, finalReferencedCaseIds, true, resolved.RelevantScripts) });
                return;
            }

            var answer = await _aiClient.AskStreamAsync(
                resolved.Prompt!,
                delta => WriteLineAsync(new { type = "delta", text = delta }),
                cancellationToken);

            var response = await CompleteAiTurnAsync(conversationId, request.Question, resolved, answer, cancellationToken);
            await WriteLineAsync(new { type = "done", response });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Chat stream cancelled by the client. ConversationId={ConversationId}", conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat stream failed. ConversationId={ConversationId}", conversationId);
            try
            {
                await WriteLineAsync(new { type = "error", message = ex.Message });
            }
            catch
            {
                // The client is already gone - nothing left to tell.
            }
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions StreamJson = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        // Thai text stays as readable UTF-8 instead of \uXXXX escapes (about half the bytes); the
        // output is never embedded in HTML, so the relaxed escaping is safe here.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // Everything that happens once a complete AI answer exists (shared by Ask and Stream): remember
    // it as the conversation's last turn, append it to the history, log it, build the response.
    private async Task<ChatResponse> CompleteAiTurnAsync(
        Guid conversationId,
        string question,
        ResolvedTurn resolved,
        string answer,
        CancellationToken cancellationToken)
    {
        // Store the underlying real question (not the shortcut word itself), so a chain like
        // "ร่างตอบลูกค้า" then "เคสซ้ำ" still resolves back to the original symptom instead of
        // searching for the literal text "ร่างตอบลูกค้า".
        _conversationStore.SetLastTurn(conversationId, new ConversationTurn(resolved.QuestionForStorage!, resolved.Context, answer));

        // Remembered only now that the turn completed (a stopped/failed request never gets here), so
        // the next turn favours the tables this answer was actually written with.
        if (resolved.SchemaTables is not null)
        {
            _conversationStore.SetSchemaTables(conversationId, resolved.SchemaTables);
        }

        var referencedCaseIds = resolved.Context.Select(c => c.Id).ToList();
        await _historyStore.AppendAsync(conversationId, resolved.QuestionForStorage!, answer, referencedCaseIds, cancellationToken);

        _logger.LogInformation(
            "Chat answered. ConversationId={ConversationId} Question={Question} ReferencedCaseIds={ReferencedCaseIds} SchemaTables={SchemaTables}",
            conversationId,
            question,
            referencedCaseIds,
            resolved.SchemaTables ?? []);

        return new ChatResponse(conversationId, answer, referencedCaseIds, false, resolved.RelevantScripts);
    }

    private sealed record ResolvedTurn(
        bool IsFinal,
        string? FinalAnswer,
        string? Prompt,
        IReadOnlyList<KbCase> Context,
        string? QuestionForStorage,
        IReadOnlyList<KnownScript> RelevantScripts,
        // Tables whose definitions this turn's prompt carries (null for turns that don't build a
        // schema section - draft/save/duplicate commands - so the conversation keeps its earlier set).
        IReadOnlyList<string>? SchemaTables = null);

    private Task<ResolvedTurn> Resolve(Guid conversationId, string question, CancellationToken cancellationToken)
    {
        var command = ChatCommandParser.Parse(question);

        if (command.Kind == ChatCommandKind.CaseLookup)
        {
            return Task.FromResult(ResolveCaseLookup(command));
        }

        return command.Kind switch
        {
            ChatCommandKind.Question => ResolveQuestion(conversationId, question, cancellationToken),
            ChatCommandKind.DuplicateCheck => Task.FromResult(ResolveSearchFollowUp(conversationId, command, topN: 20)),
            ChatCommandKind.DraftCustomerReply or ChatCommandKind.SaveCaseNote =>
                ResolveAnswerReuse(conversationId, command, cancellationToken),
            _ => throw new InvalidOperationException($"Unhandled command kind: {command.Kind}"),
        };
    }

    // How many earlier turns of this conversation get shown to the model, oldest first, on a
    // continuation or an answer-reuse shortcut - see PromptBuilder.AppendHistory. Bounded so a long
    // session's prompt (and cost/latency) stays flat instead of growing without limit; kept full-text
    // (not truncated per turn) because this app's answers already suggest real DELETE/UPDATE SQL, and
    // losing a specific fact from truncation is a worse trade than a session needing to stay under 6
    // exchanges - see git history for the "AI forgets everything before the last turn" bug this fixed
    // (IConversationStore/ChatController used to remember only a single previous turn, so บันทึกเคส
    // and any 3rd+ message in a session summarized from whatever that last turn happened to be).
    private const int MaxHistoryTurns = 6;

    private async Task<IReadOnlyList<(string Question, string Answer)>> GetRecentHistoryAsync(
        Guid conversationId, CancellationToken cancellationToken)
    {
        var turns = await _historyStore.GetConversationAsync(conversationId, cancellationToken);
        return turns
            .TakeLast(MaxHistoryTurns)
            .Select(t => (t.Question, t.Answer))
            .ToList();
    }

    private ResolvedTurn ResolveCaseLookup(ParsedCommand command)
    {
        var kbCase = _kbCaseRepository.GetAll().FirstOrDefault(c => c.Id == command.CaseId);
        if (kbCase is null)
        {
            return new ResolvedTurn(true, $"ไม่พบเคส #{command.CaseId} ในคลังเคส", null, [], command.CommandLabel, []);
        }

        var teamAddedNote = kbCase.Id.StartsWith("AI-", StringComparison.Ordinal)
            ? " [เคสที่ทีมสรุปเอง ไม่ใช่เคส Mantis จริง]"
            : "";
        var raw = $"เคส #{kbCase.Id}{teamAddedNote} | {kbCase.BsModule} / {kbCase.SubCategory} | {kbCase.CaseType}\n\n{kbCase.KbContent}";
        var scripts = FindRelevantScripts([kbCase], command.CommandLabel ?? string.Empty);
        return new ResolvedTurn(true, raw, null, [kbCase], command.CommandLabel, scripts);
    }

    private async Task<ResolvedTurn> ResolveQuestion(Guid conversationId, string question, CancellationToken cancellationToken)
    {
        var lastTurn = _conversationStore.GetLastTurn(conversationId);

        if (lastTurn is null)
        {
            // topN=6 keeps the prompt (and response time) reasonable while still giving the
            // model enough cases to build a full answer from.
            var context = _kbContextProvider.GetContext(question, topN: 6);
            var scripts = FindRelevantScripts(context, question);
            var schema = _dbSchemaProvider.GetSchema(
                ScriptsSql(scripts),
                SchemaContextText(question, context),
                CaseTexts(context),
                _conversationStore.GetSchemaTables(conversationId));
            var prompt = PromptBuilder.Build(question, context, knownScripts: scripts, includeAiSqlGuidance: true, dbSchema: schema.Text);
            return new ResolvedTurn(false, null, prompt, context, question, scripts, schema.Tables);
        }

        // Continuing an existing conversation: this plain message is far more likely answering
        // the previous answer's clarifying questions than starting an unrelated new topic, so
        // reuse the same context unchanged rather than paying for another search - no extra
        // tokens per turn. If the user genuinely switches topics without clicking "เริ่มใหม่", the
        // system prompt's own rules 1-2 (cite real cases, never force a fit) should make the
        // model say the KB context it has doesn't cover the new question rather than guess.
        var continuationScripts = FindRelevantScripts(lastTurn.Context, question);
        var history = await GetRecentHistoryAsync(conversationId, cancellationToken);
        // The previous answer joins the retrieved cases as a text whose SQL says where the work
        // happens: the tables in the check the AI just proposed (or the user just ran) are the ones
        // the follow-up - typically the fix - needs, so they count as strongly as a matched case's SQL.
        var continuationCaseTexts = CaseTexts(lastTurn.Context).Append(lastTurn.Answer).ToList();
        var continuationSchema = _dbSchemaProvider.GetSchema(
            ScriptsSql(continuationScripts),
            SchemaContextText(question, lastTurn.Context, lastTurn.Question + "\n" + lastTurn.Answer),
            continuationCaseTexts,
            _conversationStore.GetSchemaTables(conversationId));
        var continuationPrompt = PromptBuilder.BuildContinuation(history, lastTurn.Context, question, continuationScripts, continuationSchema.Text);
        return new ResolvedTurn(false, null, continuationPrompt, lastTurn.Context, question, continuationScripts, continuationSchema.Tables);
    }

    // Text the schema provider scans for table names: the matched scripts' SQL (strong signal) and
    // everything else that may mention tables (question, matched cases, previous turn).
    private static string ScriptsSql(IReadOnlyList<KnownScript> scripts) =>
        string.Join("\n", scripts.Select(s => s.SqlContent));

    private static IReadOnlyList<string> CaseTexts(IReadOnlyList<KbCase> context) =>
        context.Select(c => c.KbContent).ToList();

    private static string SchemaContextText(string question, IReadOnlyList<KbCase> context, string? previousTurn = null) =>
        question + "\n" + previousTurn + "\n" + string.Join("\n", context.Select(c => c.KbContent));

    private IReadOnlyList<KnownScript> FindRelevantScripts(IReadOnlyList<KbCase> context, string question) =>
        KnownScriptMatcher.FindRelevant(_knownScriptRepository.GetAll(), context, question);

    // Only เคสซ้ำ uses this now (see ChatCommandParser) - never wants AI-authored SQL, just a wide
    // fresh search to count how often a symptom recurs.
    private ResolvedTurn ResolveSearchFollowUp(Guid conversationId, ParsedCommand command, int topN)
    {
        var effectiveQuestion = command.RemainderText ?? _conversationStore.GetLastTurn(conversationId)?.Question;
        if (effectiveQuestion is null)
        {
            return new ResolvedTurn(
                true,
                $"พิมพ์ \"{command.CommandLabel}\" ตามหลังคำถามได้เลย หรือถามคำถามก่อนแล้วค่อยพิมพ์ \"{command.CommandLabel}\" ตามครับ",
                null,
                [],
                null,
                []);
        }

        var context = _kbContextProvider.GetContext(effectiveQuestion, topN);
        var scripts = FindRelevantScripts(context, effectiveQuestion);
        return new ResolvedTurn(false, null, PromptBuilder.Build(effectiveQuestion, context, command.CommandLabel, scripts), context, effectiveQuestion, scripts);
    }

    private async Task<ResolvedTurn> ResolveAnswerReuse(Guid conversationId, ParsedCommand command, CancellationToken cancellationToken)
    {
        var lastTurn = _conversationStore.GetLastTurn(conversationId);
        if (lastTurn is null)
        {
            return new ResolvedTurn(
                true,
                $"ยังไม่มีคำถามก่อนหน้าในบทสนทนานี้ให้ \"{command.CommandLabel}\" ครับ ถามคำถามก่อนแล้วค่อยพิมพ์คำสั่งนี้ตาม",
                null,
                [],
                null,
                []);
        }

        var scripts = FindRelevantScripts(lastTurn.Context, lastTurn.Question);
        var history = await GetRecentHistoryAsync(conversationId, cancellationToken);
        var prompt = PromptBuilder.BuildFollowUp(history, lastTurn.Context, command.CommandLabel, scripts);
        return new ResolvedTurn(false, null, prompt, lastTurn.Context, lastTurn.Question, scripts);
    }
}
