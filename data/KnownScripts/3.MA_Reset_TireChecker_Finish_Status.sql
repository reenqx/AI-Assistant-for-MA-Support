-- ============================================================================
-- 3.MA_Reset_TireChecker_Finish_Status  (แก้ไขข้อมูลจริง - แก้ค่าในตาราง)
-- รีเซ็ตสถานะ "Checker จบแล้ว" ของ Loading No. หนึ่งใบ ให้กลับเป็นยังไม่จบ
-- โดยล้างวันที่/ผู้ยืนยันการจบใน TiresChecker และ PlanTracking เป็นค่าว่าง (NULL)
-- ก่อนรัน: เช็คด้วย 01.Query_TiresChecker ว่า Loading No. ถูกต้อง และสถานะปัจจุบันเป็นอย่างที่คิด
-- คำเตือน: ค่าเดิมที่ถูกล้างเป็น NULL แล้วกู้คืนไม่ได้ ควรจดค่าเดิมไว้ก่อน
-- ============================================================================

-- Loading No. ที่ต้องการรีเซ็ต
-- ค่าด้านล่างเป็นตัวอย่างเก่า ต้องเปลี่ยนเป็น Loading No. ของเคสจริงทุกครั้งก่อนรัน
DECLARE @LoadingNo VARCHAR(20) = 'LONK4000285986'


-- TiresChecker: ล้างวันที่ Checker จบ และการยืนยันโหลดเสร็จ (Flag / วันที่ / ผู้ยืนยัน)
UPDATE [MoCS].[dbo].[TiresChecker]
SET
	 [CheckerFinishDate] = NULL
	,[ConfirmLoadingCompleteFlag] = NULL
	,[ConfirmLoadingCompleteDate] = NULL
	,[ConfirmLoadingCompleteBy] = NULL
WHERE [LoadingNo] = @LoadingNo

-- PlanTracking: ล้างวันที่จบ Checker (ActOperation5) และผู้บันทึก (ActOperation5By)
UPDATE [MoCS].[dbo].[PlanTracking]
SET
	 [ActOperation5] = NULL
	,[ActOperation5By] = NULL
WHERE [LoadingNo] = @LoadingNo
