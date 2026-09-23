using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.KnowledgeBase;
using Microsoft.AspNetCore.Mvc;

namespace AIforMAsupport.Api.Controllers;

// "เพิ่มเข้าคลังเคส" - lets the team promote a reviewed "บันทึกเคส" entry into the searchable KB
// pool, so a future question with similar wording surfaces it the same way a real Mantis case
// would. See CLAUDE.md's KB-additions scope entry and CompositeKbCaseRepository.
[ApiController]
[Route("api/[controller]")]
public sealed class KbAdditionsController : ControllerBase
{
    private readonly IKbAdditionStore _store;

    public KbAdditionsController(IKbAdditionStore store)
    {
        _store = store;
    }

    public sealed record AddKbAdditionRequest(
        string? BsModule,
        string? SubCategory,
        string? CaseType,
        string Summary,
        string? KbContent,
        int? SourceSavedCaseId);

    [HttpPost]
    public async Task<ActionResult<KbAdditionRecord>> Add([FromBody] AddKbAdditionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
        {
            return BadRequest("Summary is required.");
        }

        // KbContent is what the keyword search and the AI prompt actually read - default it to
        // Summary when the caller doesn't send a separate one, same as how a real KB case's own
        // KB_Content column already folds every other field into one searchable string.
        var kbContent = string.IsNullOrWhiteSpace(request.KbContent) ? request.Summary : request.KbContent;

        var added = await _store.AddAsync(
            request.BsModule?.Trim() ?? string.Empty,
            request.SubCategory?.Trim() ?? string.Empty,
            request.CaseType?.Trim() ?? string.Empty,
            request.Summary.Trim(),
            kbContent.Trim(),
            request.SourceSavedCaseId,
            cancellationToken);

        return Ok(added);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KbAdditionRecord>>> List(CancellationToken cancellationToken)
    {
        var items = await _store.ListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var deleted = await _store.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
