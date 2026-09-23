using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AIforMAsupport.Api.Controllers;

[ApiController]
[Route("api/sql")]
public sealed class SqlController : ControllerBase
{
    private readonly ISqlScriptRunner _runner;
    private readonly ILogger<SqlController> _logger;

    public SqlController(ISqlScriptRunner runner, ILogger<SqlController> logger)
    {
        _runner = runner;
        _logger = logger;
    }

    // Live, read-only execution against the real database - either a pre-vetted KnownScript by
    // name, or (since the user explicitly chose to allow it) raw SQL text such as what the AI
    // wrote itself and marked "-- [AI-SQL:UNVERIFIED]". Always a human clicking "รันจริง" - the
    // AI/chat pipeline never calls this endpoint on its own. Write statements are still always
    // rejected regardless of which path supplied the SQL - see SqlScriptRunner.
    [HttpPost("run")]
    public async Task<ActionResult<SqlRunResponse>> Run([FromBody] SqlRunRequest request, CancellationToken cancellationToken)
    {
        if (request is null || (string.IsNullOrWhiteSpace(request.ScriptName) && string.IsNullOrWhiteSpace(request.SqlText)))
        {
            return BadRequest();
        }

        try
        {
            var result = await _runner.RunAsync(
                request.ScriptName,
                request.SqlText,
                request.Parameters ?? new Dictionary<string, string>(),
                cancellationToken);
            return Ok(result);
        }
        catch (SqlRunNotAllowedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Live SQL execution failed for {ScriptName}", request.ScriptName ?? "(AI-authored SQL)");
            return StatusCode(502, new { error = "เชื่อมต่อหรือรันคำสั่งกับฐานข้อมูลจริงไม่สำเร็จ: " + ex.Message });
        }
    }
}
