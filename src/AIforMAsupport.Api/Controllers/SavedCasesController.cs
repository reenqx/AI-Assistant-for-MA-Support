using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.SavedCases;
using Microsoft.AspNetCore.Mvc;

namespace AIforMAsupport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SavedCasesController : ControllerBase
{
    private readonly ISavedCaseStore _store;

    public SavedCasesController(ISavedCaseStore store)
    {
        _store = store;
    }

    public sealed record SaveCaseRequest(
        Guid ConversationId,
        string Summary,
        IReadOnlyList<string>? ReferencedCaseIds,
        IReadOnlyList<string>? KnownScripts);

    // The summary saved here is whatever the person finalized in the save dialog - it may differ
    // from what the AI originally wrote (see console.js's edit-before-save flow), so this never
    // re-derives it from ConversationId; it just persists exactly what's sent.
    [HttpPost]
    public async Task<ActionResult<SavedCase>> Save([FromBody] SaveCaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            return BadRequest("Summary is required.");
        }

        var saved = await _store.SaveAsync(
            request.ConversationId,
            request.Summary.Trim(),
            request.ReferencedCaseIds ?? [],
            request.KnownScripts ?? [],
            cancellationToken);

        return Ok(saved);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SavedCase>>> List([FromQuery] int limit, CancellationToken cancellationToken)
    {
        var effectiveLimit = limit is > 0 and <= 200 ? limit : 50;
        var cases = await _store.ListAsync(effectiveLimit, cancellationToken);
        return Ok(cases);
    }
}
