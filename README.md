# AI Assistant for MA Support

เปลี่ยนเคส MANTIS ที่ปิดแล้ว ให้เป็นผู้ช่วยวิเคราะห์ปัญหาของทีม MA อัตโนมัติ

## Project Overview

**AI Assistant for MA Support** เป็นระบบผู้ช่วยอัจฉริยะสำหรับทีม Maintenance (MA) ที่นำข้อมูลจากเคสเก่าในระบบ Mantis กว่า 2,180 เคส (ตั้งแต่ปี 2020–2026) มาใช้วิเคราะห์และเสนอแนวทางแก้ไขปัญหา ระบบสามารถเชื่อมต่อกับฐานข้อมูลจริงเพื่อตรวจสอบข้อมูล (SELECT) และรันคำสั่งแก้ไข (UPDATE/DELETE) โดยมีระบบ Guardrails ที่เข้มงวดเพื่อความปลอดภัยสูงสุดภายใต้คอนเซปต์ **"AI เสนอ - MA ตัดสินใจ"**

## Key Features

* **Smart Case Search:** ค้นหาอาการจากคลังเคสเก่าด้วยภาษาพูดปกติ (Keyword Search บน In-memory CSV) พร้อมแสดงเคสที่เกี่ยวข้องทันทีภายใน <1 วินาที
* **SQL Studio & Safe Execution:** รันสคริปต์ SQL จริงผ่านหน้าเว็บ โดยบังคับกระบวนการ **"เช็คก่อนแก้"** (AI ต้องเสนอ SELECT ก่อนเสมอ) และต้องให้มนุษย์กดยืนยันทุกครั้งที่แก้ไขข้อมูลจริง
* **4 Shortcuts:**
* `#<เลขเคส>`: ดึงเนื้อหาเคสเต็มแบบดิบเพื่อดูข้อมูลอ้างอิง
* `ร่างตอบลูกค้า`: ร่างข้อความสุภาพพร้อมส่งผู้แจ้ง โดยตัดข้อมูลเชิงเทคนิค (Table/SQL/IP) ออกทั้งหมด
* `บันทึกเคส`: สรุปขั้นตอนแนวทางแก้ไขที่สำเร็จเพื่อนำไปอัปเดตลง Mantis
* `เคสซ้ำ`: สรุปความถี่ของปัญหาและดึงเลขเคสเพื่อใช้ประกอบการทำ CR (Change Request)


* **Knowledge Base Feedback Loop:** นำเคสที่แก้ปัญหาเสร็จสิ้นแล้วเพิ่มกลับเข้าสู่คลังข้อมูล (ติดแท็ก #AI) เพื่อให้ระบบครอบคลุมปัญหาใหม่ๆ เสมอ

## Architecture & Technologies

* **Backend:** ASP.NET Core 8 Web API
* **Frontend:** Razor Pages + JavaScript (UI แบบ Split-pane แชทซ้าย / พาเนลขวา)
* **AI Engine:** Claude Code CLI (Headless mode) เชื่อมต่อผ่าน Standard I/O (stdin/stdout)
* **Databases:**
* **SQL Server:** ฐานข้อมูลจริง (MoCS) สำหรับรันสคริปต์ตรวจสอบและแก้ไข
* **SQLite:** เก็บประวัติการสนทนาและ Audit Log
* **CSV (In-Memory):** จัดเก็บคลังเคสเก่า 2,180 เคส เพื่อความรวดเร็วในการค้นหา



## Installation & Setup

### Prerequisites

1. .NET 8 SDK
2. Node.js และ npm (สำหรับ Claude Code CLI)
3. ติดตั้ง Claude Code CLI และทำการล็อกอิน (`claude login`) ให้เรียบร้อย
4. SQL Server (สำหรับเชื่อมต่อฐานข้อมูลระบบ)

### ขั้นตอนการรันระบบ

เปิด Terminal 2 หน้าต่างเพื่อรันแยกส่วน API และ Web (หรือกด F5 ใน Visual Studio เพื่อรันผ่านไฟล์ `.slnLaunch`):
```bash
# Terminal 1: รัน Backend API (พอร์ต 5299)
dotnet run --project src/AIforMAsupport.Api

# Terminal 2: รัน Frontend Web (พอร์ต 5294)
dotnet run --project src/AIforMAsupport.Web

```



## Security & Guardrails

ระบบถูกออกแบบมาให้ปลอดภัยต่อการใช้งานร่วมกับ Production Database ระดับสูง:

* **No Auto-Execution:** AI ไม่มีสิทธิ์รันคำสั่ง SQL เองเด็ดขาด ต้องรอผู้ใช้งานกดยืนยันทุกครั้ง
* **Restricted Mode:** รัน Claude ด้วยแฟล็ก `--restricted` ตัดสิทธิ์ Bash/Edit ป้องกันการเข้าถึงหรือแก้ไขไฟล์ในเซิร์ฟเวอร์
* **Strict SQL Parsing:**
* คำสั่งแก้ไขข้อมูลจะมีปุ่มสีแดง และแจ้งเตือนให้ Confirm ซ้ำ
* คำสั่งที่พยายามแก้ไขข้อมูลจะถูกรันภายใต้ `SqlTransaction` เสมอ หากเกิดข้อผิดพลาดจะเกิดการ Rollback อัตโนมัติ
* หากสคริปต์มีช่องว่าง (`<...>`) ที่ยังไม่ระบุค่าจริง ปุ่มรันจะถูก Disable ทันที



## Team

**Rapid Development Section** (ICONEXT AI Challenge Day 2026)

* Nurin Khankaew
* Panyaphorn Phromchat
