# MoCS Database Schema Reference (คำอธิบายประกอบ)

ไฟล์นี้ถูกส่งเข้า prompt ทุกครั้งที่ AI ต้องแต่ง SQL เก็บเฉพาะ **กติกาการอ้างชื่อ** และ **ความหมายของคอลัมน์ที่ยืนยันจากสคริปต์จริง** ส่วนโครงสร้างตารางเต็ม (คอลัมน์ ชนิดข้อมูล PK/FK ของทุกตาราง) อยู่ใน `MoCS-Schema-Full.md` ซึ่งสร้างจากฐานข้อมูลจริง และระบบจะเลือกเฉพาะตารางที่เกี่ยวข้องกับคำถามมาใส่ให้ต่อท้าย

## กติกาการอ้างชื่อ database

Known Scripts เขียนแบบ `[MoCS].[schema].[table]` (three-part name) แต่ระบบต่อจริงอาจชี้ไปที่ database ชื่ออื่น (เช่น `MoCS_dev`) — SQL ที่แต่งใหม่ควรอ้างแบบ `[schema].[table]` (two-part name, ไม่ใส่ชื่อ database) เพื่อให้ใช้ได้กับ database ที่ connection ชี้ไปอยู่ตอนนั้นเสมอ ไม่ต้องแก้ตามชื่อ database

`MoCS_dev` เป็น dev copy ของ `MoCS` โครงสร้างตารางเหมือนกัน คนละชื่อ database เท่านั้น

## ความหมายของคอลัมน์ (ยืนยันจาก Known Scripts)

- `dbo.PlanTracking` — 1:1 กับ `LoadingNo` — `ActOperation4` = วันเริ่ม Checker, `ActOperation5` = วันจบ Checker, `ActOperation5By` = ผู้บันทึกการจบ
- `dbo.TiresChecker` — `CheckerFinishDate` = วันที่ Checker จบ, `ConfirmLoadingCompleteFlag` / `ConfirmLoadingCompleteDate` / `ConfirmLoadingCompleteBy` = การยืนยันโหลดเสร็จ
- `dbo.TiresCheckerPlan` — `LoadingQTY` = จำนวนยางตามแผนที่ต้องโหลด
- `dbo.TiresCheckerScanLog` — `ScanTireFlag = 1` = ยางเส้นที่สแกนสำเร็จ
- `dbo.TiresCheckerSticker` — `IsUsed = 1` = สติ๊กเกอร์ที่ถูกใช้แล้ว, `StickerPrepareQTY` = จำนวนที่เตรียมไว้
- `mobile.Routing` เชื่อมกับ `mobile.RoutingJob` ด้วย `RoutingID` และ `RoutingJob` มี `LoadingNo`
- `fcs.EstimateCost` เชื่อมกับ `fcs.EstimateCostDetail` ด้วย `EstimateCostId` และ `EstimateCostDetail` มี `LoadingNo`
- `logs.WmsLog` — `CreatedAt` = เวลาที่บันทึก log
- object ที่ชื่อขึ้นต้นด้วย `vw` เป็น View (ดูกฎข้อ 9 ใน System Prompt)
