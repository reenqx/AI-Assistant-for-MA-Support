using System.Text;
using AIforMAsupport.Api.Models;
using AIforMAsupport.Api.Services.KnownScripts;

namespace AIforMAsupport.Api.Services.Chat;

public static class PromptBuilder
{
    // Plain question (fresh search) and เคสซ้ำ, which also searches fresh but never wants
    // AI-authored SQL. Question-answering turns include the AI-SQL guidance below; เคสซ้ำ doesn't
    // (see ChatController).
    public static string Build(
        string question,
        IReadOnlyList<KbCase> context,
        string? commandLabel = null,
        IReadOnlyList<KnownScript>? knownScripts = null,
        bool includeAiSqlGuidance = false,
        string? dbSchema = null)
    {
        var sb = new StringBuilder();
        AppendContext(sb, context);
        AppendKnownScripts(sb, knownScripts);

        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("[คำถามจากทีม MA]");
        sb.AppendLine(question);

        if (commandLabel is not null)
        {
            sb.AppendLine();
            sb.AppendLine("[คำสั่ง]");
            sb.AppendLine(commandLabel);
        }

        if (includeAiSqlGuidance)
        {
            AppendAiSqlInstruction(sb, dbSchema);
        }

        return sb.ToString();
    }

    // Lets the model write brand-new SQL for a scenario none of the whitelisted [Known Scripts]
    // cover. Any such novel query must open with the exact marker line below as the first line of
    // its code block, so SimpleMarkdown (Web project) can detect it and render a distinct
    // "AI-generated, unverified" warning card - with its own "รันจริง" button for SELECT-only SQL
    // (the user explicitly chose to allow this; a write statement still never gets one) - instead
    // of a plain code block with no button at all. Included on every question-answering turn now
    // (not gated behind a shortcut word) since the decision-tree default answer is the only place
    // SQL guidance shows up in chat text anymore - staying dormant costs nothing when the model
    // doesn't need it. Deliberately insistent (no "skip if unsure" escape hatch, explicit "do not
    // tell the user to go run it manually" instruction) because in practice the model kept
    // forgetting the marker and, worse, telling the user it has no way to execute anything and to
    // go use SSMS themselves - which is wrong once this marker exists, since the app can already
    // run it for them with one click.
    private static void AppendAiSqlInstruction(StringBuilder sb, string? dbSchema)
    {
        sb.AppendLine();
        sb.AppendLine("[กติกาเพิ่มเติมสำหรับ SQL ที่อ้างถึงในคำตอบ - บังคับ ไม่ใช่ทางเลือก]");
        sb.AppendLine("""
            ไม่ว่าจะมีสคริปต์ใน [Known Scripts] ที่ครอบคลุมสถานการณ์นี้หรือไม่ ก็แต่ง SQL เองได้ตามปกติทั้งขั้นตรวจสถานะและขั้นแก้ไข (ไม่ต้องงดเขียน SQL เพียงเพราะมีสคริปต์จริงที่ทำหน้าที่คล้ายกันอยู่แล้ว) แต่ต้องแปะหมายเหตุกำกับเสมอเมื่อเกี่ยวข้อง:
            - ขั้นตรวจสถานะ: ถ้ามีสคริปต์ตรวจสถานะที่ครอบคลุมกว้างกว่า SQL ที่แต่งขึ้น (เช่น มีสคริปต์ตรวจ Loading ครบทุกตารางอยู่แล้ว แต่ SQL ที่แต่งเองแคบกว่านั้น) ให้ต่อท้ายว่า "ดูสถานะครบทุกตารางได้ที่แท็บ SQL Studio ด้วยสคริปต์ <ชื่อสคริปต์ตรงตามที่แสดงใน SQL Studio>" เผื่อผู้ใช้อยากเห็นภาพรวมมากกว่านี้
            - ขั้นแก้ไข: ถ้า SQL ที่แต่งขึ้นตรงกับหรือทำหน้าที่เดียวกับ Known Script ตัวใดตัวหนึ่ง ต้องหมายเหตุกำกับชื่อสคริปต์นั้นเสมอ เช่น "(ตรงกับสคริปต์จริง <ชื่อสคริปต์> ที่มีอยู่แล้ว)" ต่อท้ายบรรทัดคำอธิบายของบล็อกนั้น - ถ้าไม่ตรงกับสคริปต์ไหนเลยก็ไม่ต้องหมายเหตุ
            """);
        sb.AppendLine("ถ้าไม่มีสคริปต์ใน [Known Scripts] ข้างต้นที่ตรงกับสถานการณ์นี้พอดี แต่ต้องใช้ SQL ประกอบคำตอบ ให้แต่ง SQL ขึ้นใหม่เองได้เลยตามความเหมาะสม ไม่ต้องกังวลว่าจะรันเองไม่ได้ - ระบบมีปุ่ม \"รันจริง (read-only)\" ให้ผู้ใช้กดรันเองได้ทันทีถ้า SQL เป็น SELECT ล้วนและมีมาร์กเกอร์บรรทัดนี้เป๊ะเป็นบรรทัดแรกสุดในบล็อกโค้ด (ห้ามลืมเด็ดขาด ไม่มีข้อยกเว้น แม้จะมั่นใจในสคริปต์แค่ไหนก็ตาม):");
        sb.AppendLine("-- [AI-SQL:UNVERIFIED]");
        sb.AppendLine("""
            เหตุผลที่ต้องมี: ผู้ใช้ต้องแยกออกให้ได้ว่า SQL ก้อนไหนมาจากสคริปต์จริงที่ทีมตรวจสอบแล้ว กับก้อนไหนที่ AI แต่งขึ้นเองและยังไม่ผ่านการตรวจสอบ - ถ้าลืมใส่บรรทัดนี้ ผู้ใช้จะเข้าใจผิดว่าเป็นสคริปต์ที่ปลอดภัยเท่าของจริง และปุ่ม "รันจริง" จะไม่ขึ้นให้เลย
            ข้อสำคัญ: ห้ามบอกผู้ใช้ว่า "ผมรันเองไม่ได้ รบกวนรันเองผ่าน SSMS" หรือทำนองนี้เด็ดขาด - ถ้าใส่มาร์กเกอร์ถูกต้อง ผู้ใช้กดปุ่มในแอปรันได้เลยโดยไม่ต้องออกไปเปิด SSMS เอง ให้บอกแค่ว่า "กดปุ่มรันจริงด้านล่างเพื่อตรวจสอบ" พอ
            """);
        sb.AppendLine("""

            [ลำดับการทำงานเมื่อต้องแก้ไขข้อมูล - บังคับ]
            1. ถ้าการแก้ไขต้องรู้สถานะปัจจุบันในฐานข้อมูลก่อน (เช่น ต้องเห็นว่ามี record อะไรอยู่ จำนวนเท่าไร) ให้ตอบรอบแรก "เฉพาะ" อธิบายสั้นๆ ว่าต้องเช็คอะไรบ้าง พร้อม SQL แบบ SELECT อ่านอย่างเดียวสำหรับเช็ค (ดูกติกาเรื่องหมายเหตุอ้างอิงสคริปต์ด้านบนด้วย) - ห้ามใส่คำสั่ง UPDATE/DELETE/INSERT มาในรอบนี้ไม่ว่ากรณีใด
            2. ผู้ใช้จะกดรันจริงเอง ดูผลบนหน้าจอ แล้วถ้าต้องการให้คุณดูต่อ ผู้ใช้จะกดปุ่มส่งผลลัพธ์กลับมาหาคุณเอง (ระบบไม่ส่งให้อัตโนมัติ) เป็นข้อความที่ขึ้นต้นว่า "รันจริงกับฐานข้อมูลแล้ว ได้ผลลัพธ์ดังนี้" - เมื่อได้รับข้อความนี้ ให้อ่านผลลัพธ์จริง สรุปว่าสถานะใน DB ตอนนี้เป็นอย่างไร แล้วค่อยเสนอคำสั่งแก้ไข (UPDATE/DELETE/INSERT) ที่ปรับตามข้อมูลจริงที่เห็น (มี SELECT ตรวจคู่ตามกฎ 6) ให้ผู้ใช้กดรันเอง
            3. SQL ที่ให้ผู้ใช้กดรันต้องใส่ค่าจริงให้ครบ - ถ้ายังไม่รู้ค่า (เช่น วันที่ LoadingNo GTCode) ให้ "ถามผู้ใช้ก่อนเป็นข้อความ" ห้ามทิ้ง placeholder แบบ <วันที่> หรือค่าว่างไว้ใน SQL ที่ตั้งใจให้รัน เพราะระบบจะปิดปุ่มรันจริงให้เองเมื่อยังมี placeholder หรือค่าว่างเหลืออยู่ ผู้ใช้จะกดรันไม่ได้
            """);

        sb.AppendLine("""

            [ข้อกำหนดของบล็อก SQL ที่ให้กดรัน - บังคับ]
            - ทุกบล็อก SQL ที่แต่งเองต้องขึ้นต้นด้วยบรรทัด -- [AI-SQL:UNVERIFIED] เสมอ รวมถึงบล็อก UPDATE/DELETE ในขั้นที่ 2 ที่ตามหลัง SELECT เช็ค (ห้ามลืมเฉพาะบล็อกหลัง)
            - ห้ามใส่ BEGIN TRAN / COMMIT / ROLLBACK ในบล็อก SQL: ระบบห่อ transaction ให้เองและย้อนกลับอัตโนมัติเมื่อผิดพลาด (ถ้าเป็นบรรทัดเดี่ยว ๆ ระบบตัดออกให้ แต่ถ้าอยู่ในบรรทัดอื่นจะถูกปฏิเสธ) ให้ใช้ SELECT @@ROWCOUNT ตรวจจำนวนแถวหลังแก้แทน (นี่แทนรูปแบบ BEGIN TRAN ในกฎ 6 ของคำสั่งระบบสำหรับแอปนี้)
            - แยก SELECT เช็คกับคำสั่งแก้ไขเป็นคนละบล็อก: ให้ SELECT ก่อน ผู้ใช้กดรันดูผล แล้วจึงให้บล็อกคำสั่งแก้ไข
            """);

        if (!string.IsNullOrWhiteSpace(dbSchema))
        {
            sb.AppendLine();
            sb.AppendLine("[โครงสร้างฐานข้อมูลอ้างอิง - ตารางที่แสดงด้านล่างมีคอลัมน์ครบตามฐานจริง ให้อ้างชื่อ table/column จากตรงนี้เท่านั้น ห้ามเดาชื่อคอลัมน์ ตารางอื่นที่ไม่ได้แสดงอาจมีอยู่ในฐานแต่ยังไม่ทราบโครงสร้างในบริบทนี้]");
            sb.AppendLine(dbSchema);
        }
    }

    // Answer-reuse shortcuts (ร่างตอบลูกค้า / บันทึกเคส) that act on what was already asked and
    // already answered, rather than searching again. Neither wants AI-authored SQL: ร่างตอบลูกค้า
    // bans table/SQL names outright (3.7/§4), and บันทึกเคส only summarizes what was already used,
    // never invents anything new.
    //
    // Takes the FULL turn history (not just the immediately previous turn) - see git history for
    // the bug this fixed: ChatController used to remember only the single most recent turn per
    // conversation, so by the 3rd+ message in a session the model had already lost everything from
    // turn 1 (the original symptom), and both shortcuts summarized/drafted from whatever the last
    // turn happened to be (often just a "here's the SQL result" exchange) instead of the real case.
    public static string BuildFollowUp(
        IReadOnlyList<(string Question, string Answer)> history,
        IReadOnlyList<KbCase> context,
        string commandLabel,
        IReadOnlyList<KnownScript>? knownScripts = null)
    {
        var sb = new StringBuilder();
        AppendContext(sb, context);
        AppendKnownScripts(sb, knownScripts);

        sb.AppendLine("---");
        sb.AppendLine();
        AppendHistory(sb, history);
        sb.AppendLine("[คำสั่ง]");
        sb.AppendLine(commandLabel);

        return sb.ToString();
    }

    // Plain follow-up message within an ongoing conversation (not a shortcut command): the user
    // is very likely answering the clarifying questions the previous answer asked, not starting
    // an unrelated new topic - see git history for the case this fixed (asking "ทั้ง Loading
    // หรือเฉพาะ Serial No.?" then a plain reply naming the Loading No. searched completely fresh
    // and surfaced a different, unrelated case cluster). Reuses the same context as the previous
    // turn rather than re-searching, since a fresh search on odds-and-ends detail text (a Loading
    // No., "ทั้งหมด", etc.) drifts to unrelated cases; "เริ่มใหม่" is the explicit way to reset topic.
    // Wants the same AI-SQL treatment as a fresh question - a continuation is still part of the
    // same troubleshooting flow.
    //
    // Takes the FULL turn history so far (see BuildFollowUp's comment - same underlying bug: only
    // the single previous turn was ever shown to the model, so it "forgot" anything from earlier in
    // the same session, e.g. the original symptom once a few SQL-check round-trips had happened).
    public static string BuildContinuation(
        IReadOnlyList<(string Question, string Answer)> history,
        IReadOnlyList<KbCase> context,
        string newMessage,
        IReadOnlyList<KnownScript>? knownScripts = null,
        string? dbSchema = null)
    {
        var sb = new StringBuilder();
        AppendContext(sb, context);
        AppendKnownScripts(sb, knownScripts);

        sb.AppendLine("---");
        sb.AppendLine();
        AppendHistory(sb, history);
        sb.AppendLine("[ข้อความล่าสุดจากทีม MA - อาจเป็นการตอบคำถามที่ถามกลับไปข้างต้น หรือถามเพิ่มเติมต่อเนื่อง]");
        sb.AppendLine(newMessage);

        AppendAiSqlInstruction(sb, dbSchema);

        return sb.ToString();
    }

    // Renders every earlier turn of this conversation as a numbered transcript, oldest first, so
    // the model actually sees the whole session instead of only the single most recent exchange
    // (ChatController caps how many turns it passes in - see MaxHistoryTurns there).
    private static void AppendHistory(StringBuilder sb, IReadOnlyList<(string Question, string Answer)> history)
    {
        if (history.Count == 0)
        {
            return;
        }

        sb.AppendLine("[ประวัติการคุยก่อนหน้าในเซสชันนี้ - เรียงจากรอบแรกสุดถึงล่าสุด]");
        sb.AppendLine();
        for (var i = 0; i < history.Count; i++)
        {
            sb.AppendLine($"รอบที่ {i + 1}");
            sb.AppendLine("ทีม MA: " + history[i].Question);
            sb.AppendLine("คำตอบ: " + history[i].Answer);
            sb.AppendLine();
        }
    }

    // Explicitly states that the search already happened server-side (over all 2,180 cases) and
    // this is its result - otherwise an empty section here is ambiguous to the model between
    // "search found nothing" and "search isn't wired up," and it tries to go looking for the KB
    // itself instead of just following the system prompt's "KB ไม่มีข้อมูลเรื่องนี้" rule (rule 2).
    private static void AppendContext(StringBuilder sb, IReadOnlyList<KbCase> context)
    {
        sb.AppendLine("[เคสอ้างอิงที่เกี่ยวข้อง]");
        sb.AppendLine(context.Count > 0
            ? $"(ระบบค้นคำสำคัญในฐานข้อมูล 2,180 เคสให้แล้ว พบ {context.Count} เคสที่เกี่ยวข้องที่สุด แสดงด้านล่าง)"
            : "(ระบบค้นคำสำคัญในฐานข้อมูล 2,180 เคสให้แล้ว ไม่พบเคสที่ตรงกับคำถามนี้เลย)");
        sb.AppendLine();

        // A case whose Id starts with "AI-" came from IKbAdditionStore, not the real Mantis CSV
        // (see CompositeKbCaseRepository) - the team explicitly promoted a "บันทึกเคส" summary into
        // the searchable pool. It's still equal-weight search context (CLAUDE.md), but the model
        // must never let the reader mistake it for a case Mantis actually closed, hence the
        // per-case instruction line and the distinct label (kept separate from rule 8's
        // "[สร้างจาก AI]", which is about SQL provenance, not case provenance - conflating the two
        // would make it unclear which kind of "AI-made" a citation is warning about).
        foreach (var c in context)
        {
            var isTeamAdded = c.Id.StartsWith("AI-", StringComparison.Ordinal);
            sb.AppendLine($"เคส #{c.Id} | {c.BsModule} / {c.SubCategory} | {c.CaseType}"
                + (isTeamAdded ? " | [เคสที่ทีมสรุปเอง ไม่ใช่เคส Mantis จริง - ถ้าอ้างอิงเคสนี้ในคำตอบ ต้องใส่ป้าย [เคสที่ทีมสรุปเอง] กำกับต่อท้ายเสมอ]" : ""));
            sb.AppendLine(c.KbContent);
            sb.AppendLine();
        }
    }

    // Real script content for whatever script names showed up in the matched KB cases (or the
    // question itself) - see KnownScriptMatcher. Cases almost never paste the actual SQL, only
    // the script's name, so this is what lets the AI answer with real SQL instead of saying
    // "KB doesn't have the command" every time a Reset Tirechecker question comes up.
    private static void AppendKnownScripts(StringBuilder sb, IReadOnlyList<KnownScript>? knownScripts)
    {
        if (knownScripts is not { Count: > 0 })
        {
            return;
        }

        sb.AppendLine("[Known Scripts - สคริปต์จริงที่มีอยู่ในระบบ (ไม่ใช่จากเนื้อหาเคส แต่เป็นไฟล์ .sql จริงที่ทีม MA ใช้ ชื่อไปตรงกับที่เคสอ้างถึง)]");
        sb.AppendLine();

        foreach (var script in knownScripts)
        {
            sb.AppendLine($"สคริปต์: {script.Name}");
            sb.AppendLine($"คำอธิบาย: {script.Description}");
            sb.AppendLine("```sql");
            sb.AppendLine(script.SqlContent);
            sb.AppendLine("```");
            sb.AppendLine();
        }
    }
}
