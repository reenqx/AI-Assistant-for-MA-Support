using Microsoft.Extensions.Options;

namespace AIforMAsupport.Api.Services.KnownScripts;

public sealed class FileKnownScriptRepository : IKnownScriptRepository
{
    // Small, hand-maintained set (currently 4 files) - a short manual description per script is
    // simpler and more reliable than trying to auto-summarize SQL, and cheap to keep in sync
    // since new scripts get added to /KnownScripts rarely and deliberately.
    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1.MA_Remove_TirecheckerScanLog"] =
            "ลบ Scan Log ทั้งหมดของ Loading (ทุกเส้น/ทุก GTCode) - DELETE จาก TiresCheckerScanLog และ TiresCheckerScanStickerLog ตาม LoadingNo อย่างเดียว",
        ["2.MA_Remove_TirecheckerScanLog(some gtcode)"] =
            "ลบ Scan Log เฉพาะ GTCode ที่ระบุ ไม่กระทบ GTCode อื่นใน Loading เดียวกัน (ต้องระวังเรื่อง DoNo ตามคอมเมนต์ในสคริปต์ - REP/EXPORT ต้องระบุ DoNo ไม่งั้น GTCode นั้นจะถูก cancel จนหมด)",
        ["3.MA_Reset_TireChecker_Finish_Status"] =
            "ไม่ใช่ DELETE - เป็น UPDATE เคลียร์สถานะ \"สแกนเสร็จแล้ว\" ของ Loading กลับเป็นค่าว่าง (CheckerFinishDate, ConfirmLoadingCompleteFlag ใน TiresChecker และ ActOperation5 ใน PlanTracking) เพื่อให้ระบบมองว่ายังสแกนไม่เสร็จ",
        ["01.Query_TiresChecker"] =
            "Query ตรวจสอบสถานะ Loading ครบทุกตารางที่เกี่ยวข้อง (Plan, TiresCheckerPlan, ScanLog, PlanTracking ฯลฯ) ใช้เช็คเบื้องต้นก่อนตัดสินใจว่าจะ reset แบบไหน",
    };

    private readonly IReadOnlyList<KnownScript> _scripts;

    public FileKnownScriptRepository(IOptions<KnownScriptsOptions> options, ILogger<FileKnownScriptRepository> logger)
    {
        var directory = Path.GetFullPath(options.Value.Directory);
        if (!System.IO.Directory.Exists(directory))
        {
            logger.LogWarning("Known scripts directory not found: {Directory}", directory);
            _scripts = [];
            return;
        }

        var scripts = new List<KnownScript>();
        foreach (var file in System.IO.Directory.GetFiles(directory, "*.sql"))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var description = Descriptions.GetValueOrDefault(name, "ไม่มีคำอธิบายเพิ่มเติม - อ่านจากเนื้อ SQL โดยตรง");
            scripts.Add(new KnownScript(name, description, File.ReadAllText(file)));
        }

        _scripts = scripts;
        logger.LogInformation("Loaded {Count} known scripts from {Directory}", scripts.Count, directory);
    }

    public IReadOnlyList<KnownScript> GetAll() => _scripts;
}
