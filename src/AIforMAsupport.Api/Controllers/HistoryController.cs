using AIforMAsupport.Api.Services.History;
using Microsoft.AspNetCore.Mvc;

namespace AIforMAsupport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HistoryController : ControllerBase
{
    private readonly IConversationHistoryStore _historyStore;

    public HistoryController(IConversationHistoryStore historyStore)
    {
        _historyStore = historyStore;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummary>>> ListConversations(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        var effectiveLimit = limit is > 0 and <= 200 ? limit : 50;
        var conversations = await _historyStore.ListConversationsAsync(effectiveLimit, cancellationToken);
        return Ok(conversations);
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<IReadOnlyList<HistoryTurn>>> GetConversation(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var turns = await _historyStore.GetConversationAsync(conversationId, cancellationToken);
        if (turns.Count == 0)
        {
            return NotFound();
        }

        return Ok(turns);
    }
}
