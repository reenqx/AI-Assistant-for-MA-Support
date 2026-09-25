# AI Assistant for MA Support — Project Context

อ่านไฟล์นี้ก่อนเริ่มงาน นี่คือสรุปทุกอย่างที่คุยกันมาแล้วในแชทวางแผน (Cowork) เพื่อให้ทำงานต่อที่นี่ได้ทันทีโดยไม่ต้องอธิบายซ้ำ

## งานนี้คืออะไร

แข่ง ICN AI Day — โจทย์คือ "เอา AI มาช่วยสร้างบางสิ่งขึ้นมา แล้วสิ่งนั้นสร้างประโยชน์" ระยะเวลาสร้าง 1–2 สัปดาห์ขึ้นไป ทีมถนัด C#/.NET และจะใช้ Claude Code เป็นตัวช่วยเขียนโค้ด

**สร้างอะไร:** "AI Assistant for MA Support" (ชื่อเดิม "MA Case Copilot" — ชื่อโปรเจกต์/โฟลเดอร์/namespace ในโค้ดเปลี่ยนจาก `MACaseCopilot` เป็น `AIforMAsupport` แล้ว ส่วนชื่อที่โชว์ในแอปยังเป็น "AI Assistant for MA Support" เหมือนเดิม ไม่เกี่ยวกัน) — แชทถาม-ตอบที่ช่วยทีม MA (Application Maintenance) ของระบบ Bridgestone ค้นหาวิธีแก้ปัญหาจากเคส Mantis เก่าที่ปิดงานสำเร็จแล้ว 2,180 เคส แทนที่จะต้องเปิด Mantis ค้นเอง หรือถาม Senior

## ทำไมเลือกทำแบบนี้ (อย่าออกแบบใหม่โดยไม่ผ่านจุดนี้)

ในโฟลเดอร์นี้มีเอกสารวิสัยทัศน์ใหญ่ 2 ฉบับที่ใหญ่เกินจะสร้างจบใน 1–2 สัปดาห์:
- `MA Flow(P'Na)/AI_MA_Assistant_Proposal.docx` — ต่อ AI เข้า Database Server + Log Server จริงผ่าน Remote Agent (Phase 1–5)
- `MA Flow(P'Few)/Bridgestone_MA_Support_Flow5.docx` — Console ที่รัน Action แก้ข้อมูลจริง (Reset Tirechecker ฯลฯ) พร้อม Approval Workflow

ทั้งสองฉบับต้องขอสิทธิ์ Remote Server / ผ่าน Security Review / มี Approval หลายบทบาท — เกินเวลาแข่ง จึงตัด Scope เหลือแค่ "แชทค้นเคส" ซึ่งมีของพร้อมใช้อยู่แล้วสองไฟล์:
- `Project_Instruction_MA_Bridgestone_v2.md` — System Prompt ที่ออกแบบมาละเอียดมากแล้ว (กฎห้ามเดา, กฎอ้างอิงเลขเคสทุกประโยค, รูปแบบคำตอบ 3 โหมด, คำสั่งลัด 6 แบบ) **ใช้ตรง ๆ ไม่ต้องเขียนใหม่** (แก้ถ้อยคำเล็กน้อยตามที่ user สั่ง: กฎข้อ 1 ไม่บังคับอ้างอิงทุกประโยค, คำว่า "KB" ที่ผู้ใช้เห็นเปลี่ยนเป็น "คลังเคส" (ชื่อคอลัมน์ `KB_Content` คงเดิม), ป้ายกฎข้อ 8 เปลี่ยนเป็น `[สร้างจาก AI]`, ถอดโหมด/คำสั่ง `ขยาย` และ `เช็คเบื้องต้น` ออก, กฎข้อ 2 และส่วนทักท้วงเปลี่ยนจาก "ค้นใหม่" เป็น "ตรวจจากเคสที่ได้รับ และบอกว่ายังไม่ได้ค้นเพิ่ม" เพราะ AI ไม่มีเครื่องมือค้นเอง, เพิ่มข้อ 3.7 "จำกัดความยาว" เป้าหมาย ~1,500 ตัวอักษรไม่นับ SQL แต่ห้ามตัดเลขเคส/ป้ายอนุมาน/คำเตือน SQL — วัดแล้วคำตอบสั้นลง ~30% และเร็วขึ้นจากเฉลี่ย ~28 เป็น ~22 วินาที, กฎข้อ 6 (เดิมใช้แม่แบบ `BEGIN TRAN`/`COMMIT`/`ROLLBACK` อ้างอิงเคส #9020) แก้เป็นห้ามใส่ `BEGIN TRAN`/`COMMIT`/`ROLLBACK` ในขั้นที่ 2 แล้ว ให้ตรวจด้วย `SELECT @@ROWCOUNT` แทน เพราะเครื่องมือห่อ transaction ให้อัตโนมัติอยู่แล้วและปฏิเสธไม่ให้รันถ้ามี transaction control ซ้ำเข้ามา — ตรงกับกฎที่ `PromptBuilder`/`SqlSafetyClassifier` บังคับอยู่แล้วฝั่งโค้ด)
- `Bridgestone_KB_cleaned.csv` — เคส Mantis ปิดงานสำเร็จจริง 2,180 เคส (2020–2026) Clean พร้อมใช้ คอลัมน์: `Id, BS_Module, Sub_Category, Case_Type, Summary, Description, Steps_To_Reproduce, Additional_Information, Notes, KB_Content`

ไฟล์เสริมอื่นในโฟลเดอร์ (`info-trms-backend-*.txt`, Postman collection, SQL script ของ TMS) เป็นข้อมูลจาก Use case เดิม (LMS/TMS ของ P'Na) ไม่เกี่ยวกับ Bridgestone KB โดยตรง — ไม่ต้องใช้ในเวอร์ชันนี้

## ขอบเขต

**ทำ:**
- แชทถาม-ตอบจากคลังเคส 2,180 เคส ด้วย System Prompt เดิม
- ค้นหาแบบ Keyword/Full-text บน `KB_Content` (ห้ามพึ่ง `Sub_Category` เพราะกรอกแค่ ~22%) — KB ยังคงเป็นไฟล์ `Bridgestone_KB_cleaned.csv` เหมือนเดิม ไม่ย้ายเข้า SQL Server
- **ทุกคำถามตอบเป็นข้อความเดียวจบในกล่องแชทซ้าย** (คำตอบ AI **ทยอยขึ้นทีละส่วน** ระหว่างที่ AI เขียน ไม่รอจนจบ: Claude CLI ใช้ `--output-format stream-json --include-partial-messages` → Api `POST /api/chat/stream` (NDJSON) → Web handler `AskStream` → `console.js` `postAskStream`; ปุ่มในคำตอบที่ยังไม่จบกดไม่ได้ และปุ่ม Stop ยกเลิกได้กลางทาง — endpoint เดิม `POST /api/chat` ยังอยู่) พร้อมเนื้อหาละเอียดเต็ม (เคสอ้างอิง/SQL/ข้อควรระวัง) — เดิมมี "Decision Tree" แบบ JSON stepper กดไล่ทีละขั้นในพาเนลขวา (พัฒนาต่อจาก 2 คำสั่ง `เช็คเบื้องต้น`/`ขยาย` เดิม) แต่**ถอดออกทั้งหมดแล้ว** ทั้ง backend (ไม่ขอ JSON block จาก AI อีกต่อไป) และ UI (ไม่มีแท็บ Decision Tree เหลืออยู่ — ตัดสินใจเปลี่ยนแผนภายหลัง) พาเนลขวาเหลือ 2 แท็บ: Case Context, SQL Studio และ**ย่อ/ขยายได้**ด้วยปุ่มที่มุมขวาบน (เผื่ออยากดูแชทเต็มจอ)
- คำสั่งลัด 4 แบบที่เหลือ: `#<เลขเคส>`, `ร่างตอบลูกค้า`, `บันทึกเคส`, `เคสซ้ำ`
- Audit Log พื้นฐาน (คำถาม + เคสที่อ้างอิง)
- **SQL Studio + คำตอบแชท ต่อฐานข้อมูล production จริงแบบ read-only**: รันจริงได้กับ SQL อะไรก็ได้ที่เป็น **SELECT ล้วน** เท่านั้น (ตัดข้อจำกัด "ต้องมาจาก whitelist/Known Scripts เท่านั้น" ออกแล้ว — ตัดสินใจเปลี่ยนแผนภายหลัง) ครอบคลุมทั้ง Known Scripts จริง และ SQL ที่ AI แต่งขึ้นเอง (ดูข้อถัดไป) — ยังคงเช็ค 2 ชั้นเสมอ: (1) โค้ดสแกนหา DELETE/UPDATE/INSERT แล้วปฏิเสธ ไม่ว่า SQL จะมาจากไหน (2) DB login สิทธิ์ `db_datareader` เท่านั้น (บังคับด้วย DB permission จริง ไม่ใช่แค่โค้ด) แสดงผลลัพธ์เป็นตาราง (grid) ในหน้าเว็บ — **AI ไม่มีสิทธิ์สั่งรันเองเด็ดขาด ทุกครั้งต้องเป็นทีม MA (user) กดปุ่ม "รันจริง" เองเสมอ** ไม่มีการรันอัตโนมัติจากคำตอบแชทหรือจาก AI ไม่ว่ากรณีใด (กฎนี้ไม่เปลี่ยน แม้ตอนพิจารณาเปลี่ยนแผนรอบนี้ก็ยังยืนยันให้คงไว้)
- **AI แต่ง SQL ใหม่เองได้เสมอ ไม่ว่าจะมี Known Script ที่ครอบคลุมสถานการณ์นั้นอยู่แล้วหรือไม่ก็ตาม** (เดิม Copy-only เท่านั้น — ตัดสินใจเปลี่ยนแผนภายหลัง; เคยลองบังคับ "ห้ามแต่งเองถ้ามีสคริปต์ครอบคลุม" (ให้ตอบเป็นข้อความชี้ไปที่ SQL Studio อย่างเดียว) แต่ user ตัดสินใจเปลี่ยนกลับ เพราะยังอยากให้ AI แต่ง SQL ที่ตรงจุดของตัวเองในแชทได้เหมือนเดิม) — แทนที่ด้วยการ **แปะหมายเหตุอ้างอิง** แทน (`PromptBuilder.AppendAiSqlInstruction`): ขั้นตรวจสถานะ ถ้ามีสคริปต์ตรวจกว้างกว่า (เช่น `01.Query_TiresChecker`) ให้หมายเหตุว่าไปดูภาพรวมกว่านี้ได้ที่ SQL Studio; ขั้นแก้ไข ถ้า SQL ที่แต่งตรง/ทำหน้าที่เดียวกับ Known Script ตัวไหน ต้องหมายเหตุชื่อสคริปต์นั้นกำกับไว้เสมอ (เช่น "ตรงกับสคริปต์จริง 1.MA_Remove_TirecheckerScanLog") — กฎเดียวกันนี้ใช้ตอน `บันทึกเคส` ด้วย (แก้ใน system prompt โดยตรง) ให้ใส่ชื่อ Known Script ในสรุปที่วางลง Mantis เมื่อ SQL ที่ใช้ตรงกับสคริปต์จริง ทุกก้อนที่ AI แต่งเองต้องขึ้นต้นด้วยมาร์กเกอร์ `-- [AI-SQL:UNVERIFIED]` เพื่อให้ UI เรนเดอร์เป็นการ์ดเตือน + ปุ่มรันจริงแยกจากโค้ดปกติชัดเจนเสมอ (`SimpleMarkdown.cs`) — SQL ที่มี DELETE/UPDATE/INSERT ก็มีปุ่มรันจริงสีแดงได้ (ดูข้อ "คำสั่งแก้ไขข้อมูลรันจริงได้แล้ว" ด้านล่าง) — ช่วยให้ AI อ้างชื่อ table/column ถูกต้องด้วย 2 ไฟล์: `MoCS-Schema-Full.md` (โครงสร้างจริงทั้ง 374 ตาราง/view ดึงจากเมทาดาทาของ `MoCS_dev` ด้วย SELECT อ่านอย่างเดียว เมื่อ 2026-09-21 — ถ้าฐานเปลี่ยนให้สร้างใหม่ด้วย `tools/Export-DbSchema.ps1` แล้วรีสตาร์ท Api — สคริปต์อ่านเมทาดาทาอย่างเดียว ใช้ connection string ตัวเดียวกับ Api) ซึ่งระบบไม่ส่งเข้า prompt ทั้งไฟล์ แต่เลือกเฉพาะตารางที่เกี่ยวข้องกับคำถามต่อครั้ง (สูงสุด 8 ตาราง/7,000 ตัวอักษร (ตารางที่แค่ถูกเอ่ยลอย ๆ ไม่เกิน 3) ตั้งค่าได้ที่ `DbSchema:MaxTables`/`MaxChars`/`MaxWeakTables` และ `MaxTables=0` = ปิดการส่งตารางรายคำถามทั้งหมด — ลดจาก 20/18,000 เพราะทำให้ตอบช้า; โค้ดอยู่ที่ `FileDbSchemaProvider`; ในเซสชันเดียวกันตารางที่ใช้ในรอบก่อนถูกจำไว้และได้แต้มถ่วง และ SQL ในคำตอบก่อนหน้าของ AI นับเป็นสัญญาณแรงเท่า SQL ของเคส ดังนั้นรอบแก้ข้อมูลยังเห็นตารางเดียวกับรอบเช็ค) กับ `MoCS-Schema-Reference.md` (กติกาการอ้างชื่อ + ความหมายคอลัมน์ ส่งทุกครั้ง)
- **จำบทสนทนาทั้งเซสชัน ไม่ใช่แค่เทิร์นล่าสุด (แก้บั๊กแล้ว)** — เดิม `ChatController`/`InMemoryConversationStore` ส่งให้ AI เห็นแค่ "คำถามก่อนหน้า 1 คู่" เท่านั้นเวลาคุยต่อเนื่องหรือกด `ร่างตอบลูกค้า`/`บันทึกเคส` ทำให้คุยเกิน 2 รอบแล้ว AI ลืมอาการเดิม/ข้อมูลที่คุยไปตั้งแต่ต้น (user แจ้งเป็นบั๊ก) แก้โดยดึง **ประวัติเต็มของ session นั้นจาก `IConversationHistoryStore` (SQLite ที่มีอยู่แล้ว ใช้โชว์หน้า History) แทน** ใส่เป็น transcript เรียงรอบ ส่งให้ `PromptBuilder.BuildContinuation`/`BuildFollowUp` — จำกัดไม่เกิน **6 รอบล่าสุด** (`ChatController.MaxHistoryTurns`) เพื่อคุม token/ความเร็วให้มีเพดานตายตัว ไม่ตัดทอนเนื้อหาแต่ละรอบ (ตัดสินใจไม่ตัดทอนหลังชั่งน้ำหนักแล้วว่าแอปนี้เสนอ DELETE/UPDATE จริง ความแม่นยำสำคัญกว่าความเร็วไม่กี่วินาที) — คนละบั๊กกับฝั่ง UI: เดิม state ทั้งหมด (conversationId, ข้อความแชท, ผล SQL, แท็บขวา) อยู่ใน JS memory ล้วน ไม่มี persist เลย พอกดลิงก์ "History" (เปลี่ยนหน้าเต็มไปที่ `/History`) แล้วกดย้อนกลับ จะเห็นเหมือน "เริ่ม session ใหม่" ทั้งที่ผู้ใช้ไม่ได้กดปุ่ม New question (user แจ้งเป็นบั๊ก) แก้แล้วโดยเก็บ snapshot ของ state ลง `sessionStorage` (`console.js`, key `maAssistant.session`) ทุกครั้งที่ re-render แชท/พาเนลขวา แล้วโหลดกลับตอนเปิดหน้าใหม่ — เคลียร์เฉพาะตอนกดปุ่ม **New question** เท่านั้น (`sessionStorage` ผูกกับแท็บ ปิดแท็บถึงจะหาย ต่างจาก `localStorage` ที่ข้ามแท็บ/หน้าต่างได้ซึ่งไม่เหมาะกับ "1 session")
- **คำสั่งแก้ไขข้อมูล (DELETE/UPDATE/INSERT) รันจริงได้แล้ว** (เปลี่ยนแผนภายหลัง — user ยืนยันเองว่าเข้าใจความเสี่ยง และยืนยันว่า `MoCS_dev` เป็น copy แยกขาดจาก production จริงคนละ server/instance) เปิดด้วย config แยก `SqlRun:AllowWrites` (ปิดใน `appsettings.json` เปิดเฉพาะ Development) ห่อ transaction จริง rollback อัตโนมัติถ้า error ปุ่มเป็นสีแดง + ต้องผ่าน confirm dialog ทุกครั้ง (SQL Studio แสดงข้อความเตือนสีแดงเหนือปุ่ม ไม่มีช่องติ๊กยืนยันแล้ว — ตัดออกตามที่ user ขอ) — ยังคงต้องเป็น user กดเองเสมอ AI/Claude ไม่สั่งรันเอง
- **บล็อก SQL ในคำตอบที่ AI ลืมใส่มาร์กเกอร์ `-- [AI-SQL:UNVERIFIED]`** (เจอบ่อยกับสคริปต์แก้ไขขั้นที่ 2) ยังถูกแปลงเป็นการ์ดเดียวกันพร้อมปุ่ม Run/Edit/Copy (`RunnableSqlFormatter`) และบรรทัด `BEGIN TRAN`/`COMMIT`/`ROLLBACK` ที่อยู่เดี่ยว ๆ ถูกตัดออกก่อนจัดประเภท/รัน เพราะระบบห่อ transaction ให้เองอยู่แล้ว ส่วน transaction control ที่แทรกอยู่ในบรรทัดอื่นจะถูกปฏิเสธ (Blocked) — PromptBuilder สั่ง AI ไม่ให้ใส่ transaction control ในบล็อกที่ให้รัน
- **SQL ที่ยังมี placeholder หรือค่าว่าง รันจริงไม่ได้** (`<วันที่>`, `<LoadingNo>`, `DECLARE @x = ''`) — ปุ่มถูกปิด และ backend ปฏิเสธซ้ำอีกชั้น
- **ตัวจัดประเภท SQL 3 ชั้น (`src/Shared/SqlSafetyClassifier.cs` ไฟล์เดียวที่ทั้ง Api และ Web คอมไพล์ร่วมกัน)**: อ่านอย่างเดียว / แก้ข้อมูล (DELETE/UPDATE/INSERT/MERGE ต้อง `AllowWrites`) / **ห้ามรัน** (DROP/TRUNCATE/ALTER/CREATE/EXEC/GRANT/USE/OPENROWSET/`sp_`/`xp_`/`SELECT INTO` ฯลฯ และ SQL ที่ไม่ได้ขึ้นต้นด้วย SELECT/WITH/DECLARE/SET/DELETE/UPDATE/INSERT/MERGE จะกันการเรียกโพรซีเดอร์ตรง ๆ) — ห้ามรันเสมอ ไม่ว่าใครเขียนและตั้งค่าอะไร ตัดสินบนข้อความ **ที่จะส่งเข้า SQL Server จริง** (หลังลบบรรทัด DECLARE และ `[MoCS].`) เพราะไม่งั้นคอมเมนต์ที่เปิดก่อนบรรทัดที่ถูกลบจะปิดไม่ตรงกัน แล้วซ่อน DROP ไว้ได้ (มี Test 52 กรณี ผ่านหมด) ตัดคอมเมนต์ (ซ้อนได้) และสตริงออกก่อนหา keyword
- **แก้ SQL ก่อนรันได้**: ปุ่ม "Edit" บนการ์ด SQL (คำตอบแชท และ SQL Studio) เปิดหน้าต่าง editor — พิมพ์แก้แล้วประเมินความเสี่ยงสดผ่าน `/Index?handler=ClassifySql` (ปุ่มรันปิดจนกว่าผลของข้อความปัจจุบันจะกลับมา) รันผ่านเส้นทาง `sqlText` เดิม (Api ตรวจซ้ำทุกอย่าง) ส่วนตัวอย่าง confirm/ปุ่มแดงสำหรับคำสั่งแก้ข้อมูลยังเหมือนเดิม ตอนกด "Send this result to AI" จะแนบ SQL ที่รันไปด้วย และบอก AI ว่าผู้ใช้แก้เอง — ผลที่รันจากหน้าต่าง Edit แสดงในหน้าต่างและ **เขียนทับผลเดิมของการ์ดต้นทาง** (การ์ดในแชทหรือการ์ด SQL Studio) ไม่สร้างข้อความ/บับเบิลใหม่ในแชท และการรันจากการ์ด SQL Studio ตรง ๆ แสดงผลเฉพาะในการ์ดเหมือนเดิม — การ์ดต้นทางจะ **แสดง SQL ล่าสุดที่แก้และรันแล้ว** แทนของเดิม (มีป้าย "แก้ไขแล้ว", ปุ่มรันของการ์ดใช้ SQL นั้น, เก็บไว้กับข้อความ/สถานะแล้ววาดซ้ำได้, การ์ด SQL Studio มีปุ่ม "Reset to original script", ในหน้าต่าง Edit ปุ่ม Reset กลับเป็นต้นฉบับของ AI/สคริปต์) — รันข้อความต้นฉบับซ้ำจะล้างการแก้ไข
- **ลำดับเช็คก่อนแก้**: AI ตอบรอบแรกเป็นแค่ SELECT เช็ค (ห้ามมี UPDATE/DELETE) → user กดรันเช็ค → user กดปุ่ม "Send this result to AI" เอง (ไม่ส่งอัตโนมัติ — เปลี่ยนตามที่ user ขอ ทั้งผล SELECT และผลคำสั่งแก้ไข) → **ข้อความผลลัพธ์ที่จะส่งไปโผล่ในช่อง "พิมพ์อาการที่เจอ" ให้ก่อน (ไม่ส่งเลยทันที — เปลี่ยนตามที่ user ขอเพิ่มอีกรอบ) user ตรวจดู/แก้ไขได้แล้วกดปุ่ม Send เองอีกที** → AI สรุปสถานะจริงใน DB แล้วเสนอสคริปต์แก้ไข (กดรันได้) ตามข้อมูลจริง
- Read-only เป็นค่าเริ่มต้นของระบบ — เปิดรันแก้ไขข้อมูลได้เฉพาะเมื่อ `SqlRun:AllowWrites=true`

**ไม่ทำ (Phase ถัดไป ไม่ใช่รอบนี้):** Log Server Agent, AI/Claude สั่งรัน SQL เองโดยไม่มี user กดปุ่ม UI (ไม่ว่าอ่านหรือแก้ไข), Guided Action ที่รัน SQL แก้ข้อมูลจริง, เชื่อม Mantis สองทาง, Role/Approval Queue หลายคน

- **`บันทึกเคส` บันทึกจริงลง DB แล้ว ไม่ใช่แค่ข้อความในแชท (ทำเสร็จแล้ว)**: ตาราง SQLite ใหม่ `SavedCases` (ไฟล์ DB เดียวกับที่ `IConversationHistoryStore`/`SqliteConversationHistoryStore` ใช้อยู่แล้ว, สร้าง/อ่านผ่าน `ISavedCaseStore`/`SqliteSavedCaseStore`) เก็บ ConversationId, Summary (เนื้อหาบันทึกเคส **หลังแก้ไขแล้ว** โดย user), ReferencedCaseIds, KnownScripts, SavedAtUtc, และ `MantisIssueId` (nullable เผื่อ Phase ถัดไป) — คำตอบของคำสั่ง `บันทึกเคส` มีปุ่ม "บันทึกเคส" ต่อท้ายเสมอ (ตรวจจับจาก `isSaveCaseCommand` ใน `console.js`) กดแล้วเปิด dialog แก้ไขข้อความได้ก่อน (pattern เดียวกับตัวแก้ SQL ที่มีอยู่แล้ว, dialog `saveCaseEditor`) แล้วค่อยกด "บันทึก" ยิงเข้า DB จริงผ่าน `POST /Index?handler=SaveCase` → `SavedCasesController` (Api) — มีหน้าใหม่ `/SavedCases` (คู่กับ `/History`) แสดงรายการที่บันทึกแล้ว มี checkbox เลือกได้ (เตรียมไว้เผื่อ Phase ถัดไปจะเพิ่มปุ่ม "ส่งเข้า Mantis" ทีหลัง ตอนนี้ checkbox ใช้กับปุ่ม Copy เท่านั้น) — เทสผ่านจริงในเบราว์เซอร์ครบ flow แล้ว (ถาม → `บันทึกเคส` → กดปุ่ม → แก้ไข → บันทึก → toast ยืนยัน → เห็นในหน้า `/SavedCases`)

- **"เพิ่มเข้าคลังเคส" — เคสที่ทีมสรุปเอง (จาก `SavedCases`) ค้นเจอได้จริงในคลังแล้ว (ทำเสร็จแล้ว)**: `Bridgestone_KB_cleaned.csv` (2,180 เคสจริงจาก Mantis) **ยังไม่ถูกแก้ไข** เหมือนเดิม — เคสที่ทีมเพิ่มเข้ามาเก็บแยกไว้ในตาราง SQLite ใหม่ `KbAdditions` (ไฟล์ DB เดียวกับตารางอื่น ผ่าน `IKbAdditionStore`/`SqliteKbAdditionStore`) แล้ว **รวม** เข้ากับ CSV ตอนค้นหาเท่านั้น ผ่าน `CompositeKbCaseRepository` (registered เป็น `IKbCaseRepository` ตัวจริงใน `Program.cs`, ครอบ `CsvKbCaseRepository` เดิม + `IKbAdditionStore`) — `KeywordSearchKbContextProvider` ไม่ต้องแก้เลยเพราะเรียก `GetAll()` เหมือนเดิม เห็นทั้งสองแหล่งรวมกันอัตโนมัติ ให้คะแนนค้นหาเท่ากันทั้งสองแหล่งตามที่ตกลงไว้ (ไม่มีการหักคะแนนเคสที่ทีมเพิ่มเอง)
  - **Id เปลี่ยนจาก `int` เป็น `string` ทั้งระบบ** (KbCase.Id, ReferencedCaseIds ทุกที่, ParsedCommand.CaseId ฯลฯ — รวม 19+ ไฟล์ทั้ง Api และ Web) เพื่อให้เคสจริงใช้เลข Mantis เดิม (`"13525"`) และเคสที่ทีมเพิ่มใช้ `"AI-<n>"` (เช่น `AI-1`, `AI-2`) อยู่ในคอลัมน์เดียวกันได้โดยไม่ชนกัน — เลข `<n>` เรียงต่อจากค่าสูงสุดที่เคยมี ไม่ใช่นับจำนวนแถว จึงไม่ถูกใช้ซ้ำแม้ลบเคสเก่าทิ้งไปแล้ว (ยกเว้นตารางว่างสนิทซึ่งกลับไปเริ่มที่ 1 ใหม่ได้ ไม่ใช่บั๊ก) — `#<id>` ในแชท และปุ่ม case-pill ในคำตอบรองรับทั้งสองรูปแบบ (`CaseReferenceFormatter` regex ขยายเป็น `#(AI-\d+|\d{3,6})\b`, `ChatCommandParser` เช็ครูปแบบ id แทน `int.TryParse`)
  - **AI ต้องติดป้าย `[เคสที่ทีมสรุปเอง]` เสมอเมื่ออ้างอิงเคสกลุ่มนี้** (`PromptBuilder.AppendContext` แปะหมายเหตุกำกับต่อท้ายบรรทัดเคสที่ Id ขึ้นต้นด้วย `AI-` ในบริบทที่ส่งเข้า prompt) — ป้ายนี้คนละความหมายกับ `[สร้างจาก AI]` ในกฎข้อ 8 ของ system prompt เดิม (นั่นคือเรื่อง SQL ที่ AI แต่งเองเวลาคลังเคสไม่มีคำสั่งจริง ไม่ใช่เรื่องที่มาของตัวเคส) จงใจใช้คนละป้ายกันไม่ให้สับสน
  - **UI**: ปุ่ม "เพิ่มเข้าคลังเคส" อยู่ในส่วนรายละเอียด (หลังกดขยายแถว) ของแต่ละเคสในหน้า `/SavedCases` — กดแล้วเปิด dialog ให้แก้ไขเนื้อหา + กรอก BS_Module/Sub_Category/Case_Type ได้ก่อน (ไม่บังคับ) แล้วค่อยยืนยัน ยิงเข้า `POST /SavedCases?handler=AddToKb` → `POST /api/kbadditions` (Api) — เพิ่มซ้ำสำหรับ SavedCase เดิมจะได้ entry เดิมกลับมา ไม่สร้างซ้ำ (idempotent ผ่าน `SourceSavedCaseId`) เคสที่เพิ่มแล้วโชว์เป็นป้าย "เพิ่มแล้ว ✓ #AI-n" **ตั้งแต่แถวที่ยังไม่ขยายเลย** (chip เขียวในบรรทัด meta ข้างวันที่/แท็ก ไม่ต้องคลิกขยายก่อนถึงจะเห็น) พร้อมปุ่ม **"ลบออกจากคลัง"** ในส่วนรายละเอียด (มี confirm ก่อนลบเสมอ, ยิง `DELETE /api/kbadditions/{id}`) — ตัดสินใจทำปุ่มลบตั้งแต่รอบแรกเพราะถ้าเพิ่มเคสผิด (เช่น มี PII หลุด) จะได้ไม่ต้องไปลบตรงจาก SQLite เอง — **เพิ่มทีละหลายอันพร้อมกันได้**: ปุ่มรวมของ toolbar (ข้าง checkbox "เลือกทั้งหมด") เปลี่ยนจาก "Copy ที่เลือก" เดิม เป็น "เพิ่มเข้าคลัง (N)" (N = จำนวนที่ติ๊กไว้ อัปเดตสด) กดแล้ววนยิง `AddToKb` ทีละเคสที่ติ๊ก โดยข้ามเคสที่มีป้าย "เพิ่มแล้ว ✓" อยู่แล้ว (เช็คจาก `data-kb-added` บน checkbox) ไม่มี dialog แก้ไขทีละอันในโหมดนี้ ใช้ Summary ดิบล้วนๆ เป็น summary/kbContent ตรงๆ — ปุ่ม Copy รายแถวเดิม (คัดลอกไปวาง Mantis) ยังอยู่แยกกันคนละปุ่ม ไม่ได้ถูกแทนที่
  - เทสจริงในเบราว์เซอร์ครบ flow แล้ว: เพิ่มเข้าคลัง → ค้นเจอทันทีไม่ต้อง restart (`/Index?handler=Search`) → AI ตอบ `#AI-1` พร้อมป้ายถูกต้อง (ทั้งจากคำถามปกติผ่าน context และจากคำสั่ง `#AI-1` ตรงๆ) → ลบออกจากคลัง → ค้นไม่เจอเคสนั้นอีกทันที → เพิ่มใหม่ได้เลขถัดไปไม่ชนของเดิม → `#13525` (เคสจริง) ยังทำงานปกติไม่พัง

**โครงสร้าง Mantis จริงของทีมนี้ (ไว้อ้างอิงตอนออกแบบ `SavedCases`/เชื่อม Mantis ใน Phase ถัดไป):** โฮสต์เองที่ `mantis.iconext.cc/issuetracking/view.php?id=<เลขเคส>` (URL จริง พบฝังอยู่ในเนื้อ `Bridgestone_KB_cleaned.csv` เอง) เป็น MantisBT มาตรฐาน — ตัวอย่างไฟล์ raw export ก่อนคลีนอยู่ที่ `C:\Users\nurin.k\Documents\MA\MA Flow(P'Na)\Bridgestone MA.xml.xlsx` (sheet `Bridgestone`, 3,983 แถว x 39 คอลัมน์ ยืนยันแล้วว่าเป็นต้นทางจริงของ `Bridgestone_KB_cleaned.csv` — ตรวจกับเคส #13525/#13526 ตรงกัน) คอลัมน์ที่มี: `Id, Project, Reporter, Assigned To, Priority, Severity, Reproducibility, Status, Resolution, Category, Date Submitted, Updated, Summary, Description, Steps To Reproduce, Additional Information, Notes, Tags` (field มาตรฐานของ Mantis) บวก custom field ของทีมนี้เอง `BS_Module, Channel, Requested by, Support by, Sub Category` — ยังไม่รู้ว่า Mantis เปิด REST/SOAP API ไว้หรือไม่ ต้องขอ API access จากแอดมิน Mantis ก่อนถึงจะต่อ Phase ถัดไปได้จริง

## โครงสร้างไฟล์ในโปรเจกต์ (จัดใหม่แล้ว)

```
MA project/
├── data/                      ← ไฟล์ข้อมูล/prompt ที่แอปโหลดตอนรัน (เดิมกระจายอยู่ที่ root)
│   ├── Bridgestone_KB_cleaned.csv
│   ├── Project_Instruction_MA_Bridgestone_v2.md
│   ├── MoCS-Schema-Full.md
│   ├── MoCS-Schema-Reference.md
│   └── KnownScripts/*.sql
├── docs/
│   └── MA-Case-Copilot-Summary.docx   ← เอกสารสรุปเก่า ยังใช้ชื่อโปรเจกต์เดิม "MA Case Copilot"
│                                         และสถานะบางส่วนล้าสมัยแล้ว (เช่นยังบอกว่ายังไม่เคยทดสอบ
│                                         คำสั่งแก้ไขข้อมูล) เก็บไว้เผื่ออยากอัปเดตต่อ ไม่ได้ลบทิ้ง
├── src/
│   ├── AIforMAsupport.Api/
│   ├── AIforMAsupport.Web/
│   └── Shared/SqlSafetyClassifier.cs   ← ไฟล์เดียว compile ร่วมกันทั้ง 2 โปรเจกต์ผ่าน <Compile Include>
├── TestData/                  ← ข้อมูล/สคริปต์ทดสอบ (มี README.md ของตัวเอง)
├── tools/Export-DbSchema.ps1  ← สร้าง data/MoCS-Schema-Full.md ใหม่จาก DB จริง
├── CLAUDE.md
└── AIforMAsupport.slnx
```

Path ของทั้ง 4 ไฟล์ใน `data/` ตั้งค่าได้ที่ `appsettings.json` ของ Api ทั้งหมด (`ClaudeCli:SystemPromptFile`, `KbData:CsvFilePath`, `KnownScripts:Directory`, `DbSchema:FilePath`/`FullFilePath`) — ย้ายไฟล์จริงแล้วอัปเดต path ทั้งใน appsettings.json และ default ใน `*Options.cs` ให้ตรงกันแล้ว ไม่ต้องแก้อะไรเพิ่มถ้าไม่ย้ายที่อีก

**ทำความสะอาดแล้ว**: ลบ `bin/`/`obj/`/`.vs/` ที่ค้างอยู่ที่ root (debris จากตอนที่ folder ยังชื่อ `MACaseCopilot` ก่อนเปลี่ยนชื่อ ไม่มีไฟล์โปรเจกต์ที่ root คอยสร้างมันขึ้นมาจริง ๆ — เป็นแค่ของค้าง ลบได้ปลอดภัย regenerate เองถ้าจำเป็น)

**อัปเดตล่าสุด (2026-09-25): user สั่งให้ `appsettings.Development.json` (Api) และไฟล์ `conversation_history.db` ถูก commit ขึ้น git ได้** — เอาบรรทัด ignore ของ `appsettings.Development.json` ออกจาก `.gitignore` และเพิ่ม `!src/AIforMAsupport.Api/conversation_history.db` ยกเว้น `*.db` (ไฟล์ `*.db-shm`/`*.db-wal` ยัง ignore) — ไฟล์นี้มีรหัสผ่าน DB dev จริงเป็น plain text และไฟล์ .db มีประวัติคำถาม/เคสที่บันทึก จึงต้องให้ repo บน GitHub เป็น private เสมอ; ย่อหน้าถัดไปที่บอกว่าไฟล์นี้ถูก ignore เป็นประวัติการตัดสินใจเดิม ไม่ใช่สถานะปัจจุบันแล้ว

**`ConnectionStrings:MoCS` (รหัสผ่าน DB dev) — ตัดสินใจกลับไปกลับมารอบหนึ่ง, สรุปสุดท้าย: อยู่ใน `appsettings.Development.json` จริง ๆ ตามที่ user ยืนยัน** ทดลองย้ายไปไว้ใน `dotnet user-secrets` ก่อน (เพราะ `appsettings.Development.json` ปกติจะถูก commit เข้า git) แต่ user ขอย้ายกลับมาไว้ใน appsettings ตามเดิม — เพื่อไม่ให้รหัสผ่านหลุดเข้า git history จริง จึงเพิ่ม `src/AIforMAsupport.Api/appsettings.Development.json` เข้า `.gitignore` แบบเจาะจงไฟล์นี้ไฟล์เดียว (ไม่ใช้ pattern กว้าง เพื่อไม่ให้ไฟล์เดียวกันของ Web ที่ไม่มีความลับอะไรถูก ignore ไปด้วยโดยไม่จำเป็น) — ผลคือค่าทั้งไฟล์นี้ (รวม `SqlRun:Enabled`/`AllowWrites` ที่ไม่ใช่ความลับ) จะไม่ถูก track ใน git อีกต่อไป ถ้าเครื่องอื่นต้องตั้งค่าใหม่ ต้องสร้างไฟล์นี้เองจากตัวอย่างในเอกสารนี้ ไม่มีให้ pull จาก git — ลบ secret ที่เคยตั้งไว้ใน `dotnet user-secrets` ออกแล้วเพื่อไม่ให้มีสองที่เก็บค่าเดียวกัน

## สถาปัตยกรรมที่ตกลงแล้ว

```
MA Support พิมพ์คำถาม/คำสั่งลัด
  → Chat UI (Blazor Server)
  → ASP.NET Core Web API (C#)
  → Command Parser (ตรวจจับ #id / ร่างตอบลูกค้า / บันทึกเคส / เคสซ้ำ ฯลฯ)
  → Keyword Search (SQL Full-Text บน KB_Content, SQL Server ตาราง KB_Cases)
  → ประกอบ Context จาก Top-N เคสที่เกี่ยวข้อง
  → เรียก Claude พร้อม System Prompt เดิม + Context
  → ตอบกลับ UI + บันทึก Audit Log
```

| ชั้น | เลือกใช้ | เหตุผล |
| --- | --- | --- |
| Backend | ASP.NET Core Web API | Command Parsing, Search, เรียก Claude |
| Data | SQL Server ตาราง `KB_Cases` + Full-Text Index | Import จาก CSV ตรง ๆ ใช้ทักษะทีมที่มี ไม่ต้องเรียน Vector DB ใหม่ |
| Frontend | Blazor Server | C# ล้วน ได้ Chat Real-time ผ่าน SignalR ในตัว |
| LLM (รอบนี้) | **Claude Code CLI แบบ Headless** ผ่าน Subscription เดิม | ไม่มี Anthropic API Key แยก — ดูวิธีเรียกด้านล่าง |
| LLM (ถ้าขยายเป็นของจริง) | Anthropic Messages API (ต้องขอ API Key) | รองรับผู้ใช้พร้อมกันหลายคน เร็วกว่า |
| Stretch เผื่อเวลาเหลือ | Semantic re-rank ด้วย Embeddings | 2,180 แถวเล็กพอทำ Cosine Similarity ในหน่วยความจำได้ |

### เรียก Claude แบบไม่มี API Key (ทดสอบแล้วว่าได้จริง)

```
claude -p --output-format json --restricted \
  --system-prompt-file "data/Project_Instruction_MA_Bridgestone_v2.md" \
  < prompt_with_kb_context.txt
```

ได้ JSON กลับมา อ่านข้อความคำตอบจาก field `result` เรียกจาก C# ด้วย `Process` เขียนเข้า stdin ได้ตรง ๆ

ข้อควรระวัง: ใช้โควต้า Subscription ของคนที่ Login ไว้บนเครื่องนี้ (ไม่ใช่บิลแยก), ช้ากว่าและมี Overhead ต่อครั้งมากกว่ายิง API ตรง (พอสำหรับ Demo ไม่เหมาะขยายใช้จริงกับคนทั้งทีม), ต้องใส่ `--restricted` เสมอเพื่อปิด Bash/Edit tools ที่ Chatbot ไม่ควรมีสิทธิ์ใช้บน Server เขียน Backend คั่นด้วย interface `IAiClient` ไว้ สลับเป็น Anthropic API Key จริงทีหลังได้โดยไม่กระทบโค้ดส่วนอื่น

**บั๊กที่เจอและแก้แล้ว (สำคัญ):** ฝั่ง .NET เรียก `claude` แบบไม่ระบุนามสกุลไฟล์ ปกติจะ resolve เป็น `claude.cmd` (shim ของ npm) — แต่ `Process.Start` กับไฟล์ `.cmd` ทำให้ .NET เปิดผ่าน `cmd.exe /c` เป็นชั้นแทรกอีกชั้น ซึ่งพบว่า **ทำให้ prompt ภาษาไทยที่เขียนผ่าน stdin (แม้ตั้ง `StandardInputEncoding = UTF8` แล้ว) กลายเป็น `?` ทั้งหมดก่อนถึง Claude จริง** (การตั้งค่า UTF8 คุมแค่ pipe จาก .NET ไป cmd.exe เท่านั้น ไม่ได้คุมที่ cmd.exe ส่งต่อให้ตัว claude เอง) — พบตอนทดสอบ multi-turn ด้วยคำถามยาวที่มีภาษาไทยเยอะ ๆ ไม่ใช่ทุกครั้งจะเห็นชัด เพราะบางทีโมเดลเดายังพอตอบได้ใกล้เคียง แก้แล้วโดยให้ `ResolveExecutablePath` (`ClaudeCodeHeadlessAiClient.cs`) หา `claude.exe` ตัวจริง (native binary ที่ npm package `@anthropic-ai/claude-code` แถมมาให้ อยู่ข้าง ๆ ไฟล์ `claude.cmd`) แล้วเรียกตรง ๆ แทน ตัดชั้น cmd.exe ทิ้งไปเลย

**บั๊กที่เจอและแก้แล้ว (สำคัญมาก — ทำให้แอปค้างจริงในการใช้งานจริง):** `AskAsync`/`AskStreamAsync` เดิมเขียน prompt ทั้งก้อนลง stdin ด้วย `await WriteAsync(prompt)` **ให้เสร็จก่อน** แล้วค่อยเริ่มอ่าน stdout/stderr — ใช้ได้ตอน prompt สั้น แต่พอ prompt โตขึ้นมาก (ประวัติคุยหลายรอบ + schema หลายตาราง + ผลลัพธ์ SQL ก้อนใหญ่จากปุ่ม "Send this result to AI" รวมกันเป็นหลักหมื่น-แสนตัวอักษร) จะเกิด **pipe deadlock จริง**: pipe ของ stdin เต็ม (buffer OS มีจำกัด) ฝั่ง .NET เลยค้างรอเขียนต่อ ในขณะที่ `claude` (โหมด `--verbose` สำหรับ streaming) เริ่มพิมพ์ stdout ออกมาก่อนอ่าน stdin ครบ แต่ไม่มีใครอ่าน stdout ให้ (เพราะฝั่ง .NET ยังไม่ไปถึงบรรทัดอ่าน) ทั้งสองฝั่งเลยรอกันไปเรื่อย ๆ ไม่มี exception ไม่มี timeout โผล่เลย (เพราะ `WriteAsync(string)` ไม่มี overload รับ CancellationToken ตัว timeout 150 วินาทีเลยไม่มีผลกับจุดนี้) — เจอจากรายงานผู้ใช้ว่า "ตอบแล้วค้าง" หลังกดส่งผลรันจากสคริปต์ 01 (ซึ่ง SQL ผลลัพธ์กว้างมาก 14 ตาราง) แก้โดยให้เขียน stdin พร้อมกับอ่าน stdout/stderr (ทำเป็น Task แยกไม่ await ทันที ปล่อยให้ไหลพร้อมกัน — ดูคอมเมนต์ `WriteStdinAsync`) ทดสอบแล้วด้วย payload สังเคราะห์ ~206KB ทั้ง `/api/chat` และ `/api/chat/stream` ผ่านปกติไม่ค้าง (~15-24 วินาที เหมือนเดิม)

## คำสั่งลัด 4 แบบที่ต้อง Demo ได้ (จาก System Prompt เดิม)

`เช็คเบื้องต้น`/`ขยาย` ไม่ใช่คำสั่งแยกอีกต่อไป — ทุกคำถามธรรมดาได้เนื้อหาละเอียดเต็ม (เคสอ้างอิง/SQL/ข้อควรระวัง) รวมอยู่ในคำตอบเดียวเป็นค่าเริ่มต้นเลย ตอบเป็นข้อความล้วนในกล่องแชท ไม่มี Decision Tree แบบ JSON stepper แล้ว (ดูหัวข้อขอบเขต)

| คำสั่ง | ผลลัพธ์ |
| --- | --- |
| `#<เลขเคส>` | ดึงเนื้อเคสเต็มแบบดิบ ไม่วิเคราะห์เพิ่ม |
| `ร่างตอบลูกค้า` | ข้อความสุภาพให้ผู้แจ้ง ห้ามมีชื่อ Table/SQL/IP/Path หลุด |
| `บันทึกเคส` | สรุปวางลง Mantis ได้ทันที |
| `เคสซ้ำ` | หาว่าอาการนี้เกิดกี่ครั้ง ช่วง Id ไหน — ใช้ประกอบทำ CR |

## Demo Script (4 ฉาก ใช้ข้อมูลจริง)

1. ถามตรง (คำเดียวกับเคสจริง #13525 พิสูจน์แล้วว่าค้นเจอจริงกับระบบค้นจริง — ดู `TestData/test-questions.md` ข้อ A1) "Reset Tirechecker ทำยังไง รบกวนถอยสถานะการสแกนยางทั้งหมดให้หน่อยครับ ต้องการเริ่มสแกนใหม่ทั้งหมดครับ" → คาดหวังตอบพร้อมอ้างเคส `#13525` (ประโยคสั้น "Reset Tirechecker ทำยังไง" เฉย ๆ **ไม่พอ** — เทสจริงแล้วไม่ติดอันดับ top-5)
2. พิมพ์ `#13525` → ดึงเนื้อเคสดิบมาแสดงทันที (พิสูจน์ Shortcut ทำงานจริง)
3. ถามเรื่องที่ไม่มีใน KB จริง ๆ → ต้องตอบ "คลังเคสไม่ได้ระบุเรื่องนี้" ไม่เดา (พิสูจน์กฎ 2 ของ System Prompt)
4. พิมพ์ `ร่างตอบลูกค้า` ต่อจากฉากที่ 1 → ต้องไม่มีชื่อ Table/SQL หลุดออกมา
5. เปิดแท็บ SQL Studio (หรือกดปุ่ม "รันจริง" ใต้บล็อก SQL ในคำตอบแชท) จากฉากที่ 1 → กรอกค่าจริง (เช่น GTCode) ให้ครบ ไม่มี placeholder → กด "รันจริง (read-only)" → เห็นผลลัพธ์จริงจากฐานข้อมูล (`MoCS_dev`) ขึ้นเป็นตารางในหน้าเว็บทันที จากนั้นกดปุ่ม "Send this result to AI" เพื่อให้ AI แนะนำขั้นต่อไปตามข้อมูลจริง (พิสูจน์ว่าต่อฐานข้อมูลจริงได้ ไม่ใช่ mockup — รันได้ทั้ง Known Script และ SELECT ที่ AI แต่ง; คำสั่งแก้ไขข้อมูลเป็นปุ่มแดงต้อง confirm และเปิดเฉพาะเมื่อ `SqlRun:AllowWrites=true` ไม่ควรโชว์ในเดโมหน้ากรรมการ)

### ข้อมูลเทส/เดโม (`TestData/`)

คลังเคส (KB) ที่ใช้เดโม/เทสเป็นของจริงเสมอ (`Bridgestone_KB_cleaned.csv` 2,180 เคสจริง ค่า default ของแอป) **ไม่มีการ mock ส่วน KB** — สิ่งเดียวที่ mock คือข้อมูลใน DB (`TestData/mock-db-insert.sql` ใส่ Loading จำลอง `LONKTEST0001`–`0004` ลง DB dev ทำเองใน SSMS เท่านั้น ไม่เคยรันผ่าน Claude/แอป) เพื่อให้กด SELECT/UPDATE/DELETE ผ่านหน้าเว็บได้จริงในเดโมโดยไม่แตะข้อมูลจริง ชุดคำถามทดสอบ 36 ข้อ (`TestData/test-questions.md`/`.json`) กลุ่ม A ทุกข้ออ้างเลขเคสจริงที่ยืนยันแล้วว่าค้นเจอจริงกับระบบค้นจริง (ไม่ใช่เดา) เช็คอัตโนมัติแบบไม่เรียก AI ได้ด้วย `TestData/Run-RetrievalCheck.ps1`

## Timeline 2 สัปดาห์

**Week 1 — วางท่อข้อมูล**
- Day 1–2: Import CSV → SQL Server + Full-Text Index · ตั้ง ASP.NET Core Web API · เขียน `IAiClient` เรียก Claude Code Headless ให้ End-to-end ตอบกลับได้จริง (Hardcode เคสตัวอย่างไปก่อน)
- Day 2–3: ทดสอบ Rate Limit/เวลาตอบของ Claude Code Headless กับคำถามจริงชุดเล็ก ปรับ Prompt/Timeout ให้เสถียร
- Day 3–4: สร้าง Command Parser + Keyword Search จริง ต่อผลค้นหาเข้าเป็น Context ให้ Claude
- Day 5: สร้างหน้า Chat (Blazor) ทดสอบคำสั่งลัดทั้ง 6 แบบกับข้อมูลจริง

**Week 2 — เก็บรายละเอียด + เตรียม Demo**
- Day 6–7: ทำคำสั่งที่เหลือ (ร่างตอบลูกค้า, บันทึกเคส, เคสซ้ำ) · เพิ่ม Audit Log พื้นฐาน
- Day 8: ปรับคุณภาพการค้นหา · ถ้าเวลาเหลือเพิ่ม Semantic re-rank
- Day 9: รัน Demo Script ทั้ง 4 ฉากซ้ำ ๆ แก้ Edge Case · เตรียมสไลด์ผูกกับวิสัยทัศน์ Phase 1–5 เดิม
- Day 10: Buffer / ซ้อมพรีเซนต์ / Deploy ขึ้น Demo Environment

## ความเสี่ยงที่ต้องระวัง

- **PII ใน KB** — มีชื่อ-นามสกุลพนักงาน/ลูกค้า เบอร์โทรจริง (กฎ 3.8 ใน System Prompt) ต้องเช็คนโยบายก่อนส่ง Context เข้า Claude อาจต้อง Mask ก่อน
- **คุณภาพการค้นหาคือความเสี่ยงหลัก** ไม่ใช่ตัว Prompt (ออกแบบมาดีอยู่แล้ว) ต้องเทสกับคำถามจริงจาก Mantis ย้อนหลังก่อนวันแข่ง
- **Read-only เท่านั้น** เวอร์ชัน Demo ห้ามเขียนกลับเข้าระบบจริงแม้ผู้ใช้จะพิมพ์ขอให้แก้ข้อมูล

## Impact ที่ใช้ Pitch กรรมการ

จาก Flow5.docx ตัวอย่างเคสปี 2025+ 100 เคส: Reset Tireschecker 54, Add BSJCode 22, Duplicate Record 13, Reset Scan Log 11 — ทุกกลุ่มเป็นงานที่ MA ต้อง Remote เข้า DB เอง ค้น Mantis เอง พึ่งความจำ Senior ตัวชี้วัดที่เสนอกรรมการได้: เวลาเฉลี่ยจากถามถึงได้คำตอบแรก (เทียบ Manual vs AI-assisted) และจำนวนเคสที่ตอบได้โดยไม่ต้องถาม Senior

## เริ่มจากตรงนี้

1. Import `Bridgestone_KB_cleaned.csv` เข้า SQL Server + สร้าง Full-Text Index
2. Scaffold โปรเจกต์ ASP.NET Core Web API
3. เขียน `IAiClient` เรียก Claude Code Headless ตามคำสั่งด้านบน ทดสอบให้ตอบกลับได้จริงก่อนต่อส่วนอื่น
4. ค่อยเพิ่ม Command Parser, Keyword Search, หน้า Chat ตามลำดับใน Timeline

แผนฉบับเต็ม (มี Diagram, สไตล์ Presentation) อยู่ในเว็บอาร์ติแฟกต์ "MA Case Copilot" — ถ้าต้องการดูย้อนหลังให้ขอ URL จากแชท Cowork เดิม
