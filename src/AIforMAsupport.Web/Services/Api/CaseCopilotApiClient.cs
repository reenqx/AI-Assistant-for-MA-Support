using System.Net.Http.Json;
using System.Text.Json;

namespace AIforMAsupport.Web.Services.Api;

public sealed class CaseCopilotApiClient : ICaseCopilotApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public CaseCopilotApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ChatApiSearchResponse> SearchAsync(string question, Guid? conversationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/chat/search",
            new ChatApiRequest(question, conversationId),
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatApiSearchResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response from AI Assistant for MA Support API.");
    }

    public async Task<ChatApiResponse> AskAsync(string question, Guid? conversationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            new ChatApiRequest(question, conversationId),
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ChatApiResponse>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response from AI Assistant for MA Support API.");
    }

    // The API answers /api/chat/stream with newline-delimited JSON (see ChatController.Stream):
    // {"type":"delta","text":...} pieces, then one {"type":"done","response":{...}} - or
    // {"type":"error","message":...}. HttpCompletionOption.ResponseHeadersRead is what lets us start
    // reading while the API is still writing; HttpClient.Timeout then only covers getting the
    // headers, not the (long) time the body takes to arrive.
    public async Task<ChatApiResponse> AskStreamAsync(
        string question,
        Guid? conversationId,
        Func<string, Task> onTextDelta,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat/stream")
        {
            Content = JsonContent.Create(new ChatApiRequest(question, conversationId), options: JsonOptions),
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (line.Length == 0)
            {
                continue;
            }

            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            switch (root.GetProperty("type").GetString())
            {
                case "delta":
                    await onTextDelta(root.GetProperty("text").GetString() ?? string.Empty);
                    break;

                case "done":
                    return root.GetProperty("response").Deserialize<ChatApiResponse>(JsonOptions)
                        ?? throw new InvalidOperationException("Empty response from AI Assistant for MA Support API.");

                case "error":
                    throw new InvalidOperationException(root.GetProperty("message").GetString() ?? "AI call failed.");
            }
        }

        throw new InvalidOperationException("The answer stream ended before the answer was complete.");
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> ListHistoryAsync(int limit, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/history?limit={limit}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<ConversationSummaryDto>>(JsonOptions, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<HistoryTurnDto>?> GetHistoryConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/history/{conversationId}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IReadOnlyList<HistoryTurnDto>>(JsonOptions, cancellationToken);
    }

    public async Task<SqlRunApiResult> RunSqlAsync(string? scriptName, string? sqlText, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/sql/run",
            new SqlRunApiRequest(scriptName, sqlText, parameters),
            JsonOptions,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<SqlRunApiResponse>(JsonOptions, cancellationToken);
            return new SqlRunApiResult(true, result, null);
        }

        string? message = null;
        try
        {
            var errorBody = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions, cancellationToken);
            if (errorBody.TryGetProperty("error", out var errProp))
            {
                message = errProp.GetString();
            }
        }
        catch (JsonException)
        {
            // Error body wasn't JSON (e.g. a raw 502 from a proxy) - fall through to the generic message below.
        }

        return new SqlRunApiResult(false, null, message ?? $"HTTP {(int)response.StatusCode}");
    }

    public async Task<SavedCaseDto> SaveCaseAsync(
        Guid conversationId,
        string summary,
        IReadOnlyList<string> referencedCaseIds,
        IReadOnlyList<string> knownScripts,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/savedcases",
            new SaveCaseApiRequest(conversationId, summary, referencedCaseIds, knownScripts),
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SavedCaseDto>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response from AI Assistant for MA Support API.");
    }

    public async Task<IReadOnlyList<SavedCaseDto>> ListSavedCasesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/savedcases?limit={limit}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<SavedCaseDto>>(JsonOptions, cancellationToken);
        return result ?? [];
    }

    private sealed record AddKbAdditionApiRequest(
        string? BsModule,
        string? SubCategory,
        string? CaseType,
        string Summary,
        string? KbContent,
        int? SourceSavedCaseId);

    public async Task<KbAdditionDto> AddKbAdditionAsync(
        string? bsModule,
        string? subCategory,
        string? caseType,
        string summary,
        string? kbContent,
        int? sourceSavedCaseId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/kbadditions",
            new AddKbAdditionApiRequest(bsModule, subCategory, caseType, summary, kbContent, sourceSavedCaseId),
            JsonOptions,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<KbAdditionDto>(JsonOptions, cancellationToken);
        return result ?? throw new InvalidOperationException("Empty response from AI Assistant for MA Support API.");
    }

    public async Task<IReadOnlyList<KbAdditionDto>> ListKbAdditionsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/kbadditions", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<KbAdditionDto>>(JsonOptions, cancellationToken);
        return result ?? [];
    }

    public async Task<bool> DeleteKbAdditionAsync(string id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/kbadditions/{Uri.EscapeDataString(id)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
