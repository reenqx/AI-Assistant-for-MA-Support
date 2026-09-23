# TestData — ข้อมูลจำลองสำหรับเทสระบบ

คลังเคส (KB) ใช้ของจริงเสมอ (`Bridgestone_KB_cleaned.csv` 2,180 เคสจริง — ค่า default ของแอปอยู่แล้ว ไม่ต้องตั้งอะไรเพิ่ม) **สิ่งที่ mock มีแค่ข้อมูลใน DB** เพื่อให้มีแถวจริงให้กด SELECT/UPDATE/DELETE ผ่านหน้าเว็บได้ในเดโมโดยไม่แตะข้อมูลจริง

| ไฟล์ | ใช้ทำอะไร |
| --- | --- |
| `mock-db-insert.sql` | INSERT ข้อมูลจำลองลง DB (Loading `LONKTEST0001`–`0004`, 28 คำสั่ง / 64 แถว) |
| `mock-db-verify.sql` | SELECT ล้วน ตรวจว่าข้อมูลเข้าแล้ว |
| `mock-db-cleanup.sql` | ลบข้อมูลจำลองออก |
| `test-questions.md` / `.json` | คำถามทดสอบ 36 ข้อ กลุ่ม A–F พร้อมผลที่คาดหวัง — กลุ่ม A (ค้นเคส) ทุกข้ออ้างเลขเคสจริงและยืนยันแล้วว่าค้นเจอจริงกับระบบค้นจริง |
| `Run-RetrievalCheck.ps1` | เช็คการค้นเคสของแต่ละคำถามโดยไม่เรียก AI (ฟรี เร็ว) |

## 1) รันแอป

ไม่ต้องตั้งค่าอะไรเพิ่ม รันตามปกติ แอปจะโหลดคลังเคสจริงจาก `Bridgestone_KB_cleaned.csv` เอง:

```powershell
dotnet run --project src/AIforMAsupport.Api
```

## 2) ตรวจการค้นเคส (ไม่เรียก AI)

```powershell
powershell -ExecutionPolicy Bypass -File TestData\Run-RetrievalCheck.ps1 -ApiBase http://localhost:5299
```

ผลล่าสุด (กับคลังเคสจริง): ผ่าน 11, known issue 1 (A11b)

**ข้อจำกัดที่รู้อยู่แล้ว:** การค้นตัดคำที่ช่องว่าง/เครื่องหมายเท่านั้น ประโยคภาษาไทยที่ไม่มีช่องว่างจึงจับคู่ไม่ดี (A11b) — A11 คือคำถามเดียวกันที่เว้นวรรคปกติ ซึ่งค้นเจอ (ยืนยันแล้วว่าแค่ลบช่องว่าง 1 จุดในประโยคเดียวกันทำให้ผลลัพธ์เปลี่ยนจากเจอเคสจริงเป็น 0 ผลลัพธ์ทันที)

## 3) ข้อมูลจำลองใน DB (ทำเองใน SSMS เท่านั้น)

- Claude/แอปไม่เคยรัน `mock-db-insert.sql` และ `mock-db-cleanup.sql` — ให้ทีมรันเองใน SSMS
- รันเฉพาะบน DB dev ที่เป็นสำเนาแยกจาก production (สคริปต์เช็คชื่อ DB และกันรันซ้ำให้แล้ว)
- ลำดับ: `mock-db-insert.sql` → `mock-db-verify.sql` → ทดสอบ → `mock-db-cleanup.sql`
- ตรวจ schema กับ metadata จริงแบบออฟไลน์แล้ว แต่ยังไม่เคยรันจริง
- จำนวนแถวที่คาดหวังต่อ Loading ดูได้จาก `mock-db-verify.sql` และคอลัมน์ `expectedMockDb` ใน `test-questions.json`

## 4) เดโม SQL Studio จริง (กลุ่ม D ใน test-questions)

1. ถามคำถามในกลุ่ม A1/D1 (ใช้คำจริงจากเคส #13525 ที่มีในคลังเคสจริงอยู่แล้ว) → AI ค้นเจอเคสจริงพร้อมสคริปต์ 01/1/2/3 จริง
2. เปิด SQL Studio กรอก LoadingNo เป็นค่าจำลอง เช่น `LONKTEST0002` (ที่ insert ไว้ในขั้นตอนที่ 3 ข้างบน)
3. กด Run live — เห็นผลจากข้อมูลจำลองจริงในหน้าเว็บ ทำต่อได้ทั้ง SELECT/UPDATE/DELETE ตาม D1–D6
