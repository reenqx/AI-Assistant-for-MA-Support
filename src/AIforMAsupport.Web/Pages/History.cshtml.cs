using AIforMAsupport.Web.Services.Api;
using AIforMAsupport.Web.Services.Markdown;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIforMAsupport.Web.Pages;

// Read-only for now (per scope decision) - browsing past conversations, not resuming/continuing
// them. The audit trail this reads from is IConversationHistoryStore on the API side.
public sealed class HistoryModel : PageModel
{
    private readonly ICaseCopilotApiClient _apiClient;

    public HistoryModel(ICaseCopilotApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public IReadOnlyList<ConversationSummaryDto> Conversations { get; private set; } = [];

    public Guid? SelectedConversationId { get; private set; }

    public IReadOnlyList<HistoryTurnDto>? SelectedTurns { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(Guid? id, CancellationToken cancellationToken)
    {
        try
        {
            Conversations = await _apiClient.ListHistoryAsync(50, cancellationToken);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"โหลดประวัติไม่สำเร็จ: {ex.Message}";
            return;
        }

        if (id is { } conversationId)
        {
            SelectedConversationId = conversationId;
            try
            {
                SelectedTurns = await _apiClient.GetHistoryConversationAsync(conversationId, cancellationToken);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"โหลดบทสนทนาไม่สำเร็จ: {ex.Message}";
            }
        }
    }

    public static string RenderAnswer(string answer) => SimpleMarkdown.ToHtml(answer);
}
