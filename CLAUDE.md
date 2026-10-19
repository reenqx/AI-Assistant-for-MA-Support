# สรุปโปรเจกต์ AI Assistant for MA Support (อัปเดตล่าสุด)

## 🎯 ภาพรวมโปรเจกต์

แชทบอทช่วยทีม MA (Application Maintenance) ค้นหาวิธีแก้ปัญหาจากเคส Mantis ปิดงานสำเร็จแล้ว 2,180 เคส เพื่อลดเวลาค้นหาและลดการพึ่งพา Senior

* **Tech Stack:** ASP.NET Core Web API (Backend) + Blazor Server (Chat UI)
* **LLM:** Claude Code CLI (รันแบบ Headless)
* **Database (ตัวแอป):** SQLite (`conversation_history.db`) สำหรับเก็บประวัติแชท และเคสที่บันทึก
* **Database (ระบบเป้าหมาย):** SQL Server (`MoCS_dev`) สำหรับรันสคริปต์ตรวจสอบและแก้ไขข้อมูล

## ✨ ฟีเจอร์และขอบเขตปัจจุบัน

1. **การตอบคำถาม (Chat):**
* ตอบจบครบในข้อความเดียว (ทยอยแสดงผลแบบ Streaming)
* AI จดจำบริบทการคุยต่อเนื่องได้ **6 รอบล่าสุด** (เก็บข้อมูล Session ลง SQLite)
* มีแท็บขวา 2 แท็บ: **Case Context** (ข้อมูลเคสที่อ้างอิง) และ **SQL Studio** (สำหรับรันคำสั่ง DB)


2. **คำสั่งลัด 4 รูปแบบ:**
* `#<เลขเคส>`: ดึงเนื้อหาเคสนั้นมาแสดงตรงๆ
* `ร่างตอบลูกค้า`: เขียนข้อความตอบลูกค้าแบบสุภาพ (ซ่อนชื่อ Table/SQL)
* `บันทึกเคส`: สรุปเคสลง DB จริง (ตาราง `SavedCases`) เพื่อเตรียมนำไปใส่ Mantis มีหน้า `/SavedCases` ให้จัดการ
* `เคสซ้ำ`: หาจำนวนเคสที่เกิดอาการเดียวกัน


3. **ระบบคลังเคส (KB):**
* แหล่งที่ 1: ไฟล์ `Bridgestone_KB_cleaned.csv` (2,180 เคสจริง - ไม่ถูกแก้ไข)
* แหล่งที่ 2: เคสที่ทีมสรุปและเพิ่มเอง (บันทึกลง `KbAdditions` ใน SQLite) ได้รหัสเป็น `AI-<n>`
* *ระบบจะค้นหาจากทั้ง 2 แหล่งรวมกัน AI จะติดป้าย `[เคสที่ทีมสรุปเอง]` หากมาจากแหล่งที่ 2*



## 🛠 ระบบ SQL Studio และการรันสคริปต์ (สำคัญ)

* **AI ไม่มีสิทธิ์สั่งรัน SQL เองเด็ดขาด** ผู้ใช้ (ทีม MA) ต้องเป็นคนกดปุ่ม "รันจริง" เสมอ
* **AI สร้าง SQL ได้เอง:** หากไม่มี Known Script รองรับ AI สามารถแต่ง SQL เองได้โดยอิงจากไฟล์ Schema (`data/MoCS-Schema-*.md`) โดยสคริปต์ที่แต่งเองจะมีป้าย `-- [AI-SQL:UNVERIFIED]` เตือนเสมอ
* **การรันข้อมูล (Read/Write):**
* **SELECT:** รันได้ทันที แสดงผลเป็นตารางบนหน้าเว็บ
* **DELETE/UPDATE/INSERT:** รันได้ **เฉพาะเมื่อเปิด config `SqlRun:AllowWrites**` ระบบจะห่อ Transaction ให้ มีปุ่มสีแดง และต้องกดยืนยันก่อนเสมอ
* **ความปลอดภัย:** ระบบมีตัวสแกนบล็อกคำสั่งอันตราย (DROP, ALTER, ฯลฯ) และบล็อก SQL ที่ยังมี placeholder ว่างๆ อยู่


* **Flow การแก้ปัญหาจริง:**
1. AI เสนอ SELECT → 2. User กดรัน → 3. User กด "Send this result to AI" → 4. AI ประเมินแล้วเสนอ UPDATE/DELETE → 5. User กดรันแก้ไข



## 📂 โครงสร้างไฟล์และการตั้งค่าที่ต้องรู้

* **`data/`**: เก็บไฟล์จำเป็นตอนรัน ได้แก่
* ไฟล์คลังเคส (`Bridgestone_KB_cleaned.csv`)
* ไฟล์ System Prompt หลัก (`Project_Instruction_MA_Bridgestone_v2.md` - ห้ามแก้โครงสร้างโดยไม่จำเป็น)
* ไฟล์ Schema ฐานข้อมูล (`MoCS-Schema-Full.md`, `MoCS-Schema-Reference.md`)


* **การเรียก Claude:** แอปจะเรียก `claude.exe` ตรงๆ ผ่าน Command Line (แก้ปัญหา Deadlock รองรับ Prompt ขนาดใหญ่ได้แล้ว)
* **ความปลอดภัยของ Source Code:**
* ไฟล์ `appsettings.Development.json` (เก็บ Connection String ของ DB Dev และการเปิดสิทธิ์ AllowWrites) และ `conversation_history.db` **ถูก Ignore จาก Git แล้ว** เพื่อป้องกันความลับหลุด หาก clone ใหม่ต้องสร้างไฟล์เหล่านี้เอง
