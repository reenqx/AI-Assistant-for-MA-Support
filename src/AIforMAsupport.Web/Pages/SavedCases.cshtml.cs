using AIforMAsupport.Web.Services.Api;
using AIforMAsupport.Web.Services.Markdown;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIforMAsupport.Web.Pages;

// Read-only browse of everything saved via the "บันทึกเคส" save dialog (Index.cshtml.cs's
// OnPostSaveCaseAsync -> Api's SavedCasesController) - a curated subset of /History's full
// turn-by-turn audit trail: only the ones a person actually reviewed and clicked "บันทึก" on,
// in whatever wording they finalized there. See CLAUDE.md's SavedCases scope entry.
//
// Also hosts "เพิ่มเข้าคลังเคส"/"ลบออกจากคลัง": promoting a saved case into the searchable KB pool
// (Api's KbAdditionsController) and undoing that. [IgnoreAntiforgeryToken] because these fire from
// plain JS fetch() calls, same reasoning as IndexModel.
[IgnoreAntiforgeryToken]
public sealed class SavedCasesModel : PageModel
{
    private readonly ICaseCopilotApiClient _apiClient;
    private readonly ILogger<SavedCasesModel> _logger;

    public SavedCasesModel(ICaseCopilotApiClient apiClient, ILogger<SavedCasesModel> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public IReadOnlyList<SavedCaseDto> SavedCases { get; private set; } = [];
    public IReadOnlyList<KbAdditionDto> KbAdditions { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            SavedCases = await _apiClient.ListSavedCasesAsync(100, cancellationToken);
            KbAdditions = await _apiClient.ListKbAdditionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved cases from the AI Assistant for MA Support API.");
            ErrorMessage = $"โหลดรายการเคสที่บันทึกไว้ไม่สำเร็จ: {ex.Message}";
        }
    }

    // Null if this saved case hasn't been added to the KB pool yet - lets the view show
    // "เพิ่มเข้าคลังเคส" vs. "เพิ่มแล้ว ✓ (#AI-n)" per row without a second round trip.
    public string? FindKbAdditionId(int savedCaseId) =>
        KbAdditions.FirstOrDefault(a => a.SourceSavedCaseId == savedCaseId)?.Id;

    public sealed record AddToKbRequest(
        int? SourceSavedCaseId,
        string? BsModule,
        string? SubCategory,
        string? CaseType,
        string Summary,
        string? KbContent);

    public async Task<IActionResult> OnPostAddToKbAsync([FromBody] AddToKbRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Summary))
        {
            return BadRequest();
        }

        try
        {
            var added = await _apiClient.AddKbAdditionAsync(
                request.BsModule,
                request.SubCategory,
                request.CaseType,
                request.Summary.Trim(),
                request.KbContent,
                request.SourceSavedCaseId,
                cancellationToken);

            return new JsonResult(added);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add a case to the KB via the AI Assistant for MA Support API.");
            return StatusCode(502, new { error = $"เพิ่มเข้าคลังเคสไม่สำเร็จ: {ex.Message}" });
        }
    }

    public sealed record DeleteKbAdditionRequest(string Id);

    public async Task<IActionResult> OnPostDeleteKbAdditionAsync([FromBody] DeleteKbAdditionRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Id))
        {
            return BadRequest();
        }

        try
        {
            var deleted = await _apiClient.DeleteKbAdditionAsync(request.Id, cancellationToken);
            return new JsonResult(new { deleted });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete a KB addition via the AI Assistant for MA Support API.");
            return StatusCode(502, new { error = $"ลบออกจากคลังไม่สำเร็จ: {ex.Message}" });
        }
    }

    public static string RenderSummary(string summary) => SimpleMarkdown.ToHtml(summary);

    // A one-line label for the collapsed list row - strips a wrapping ``` fence (the AI's
    // "บันทึกเคส" replies are usually one big fenced block) and the "[ปัญหา]"-style section
    // labels, then takes the first line that actually has content.
    public static string RenderPreview(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary)) return "(ไม่มีเนื้อหา)";

        var text = summary.Trim();
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0) text = text[(firstNewline + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
        }

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? preview = null;
        foreach (var line in lines)
        {
            var cleaned = line.Trim('#', ' ', '*', '-').Trim();
            if (cleaned.Length == 0) continue;
            if (cleaned.StartsWith('[') && cleaned.EndsWith(']')) continue;
            preview = cleaned;
            break;
        }
        preview ??= "(ไม่มีเนื้อหา)";

        const int maxLen = 120;
        return preview.Length > maxLen ? preview[..maxLen] + "…" : preview;
    }
}
