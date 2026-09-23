-- ============================================================================
-- mock-db-cleanup.sql  (แก้ไขข้อมูลจริง - ลบข้อมูลจำลอง)
-- ลบเฉพาะแถวที่ Loading No. ขึ้นต้นด้วย LONKTEST ที่ mock-db-insert.sql เพิ่มไว้ (และที่ทดสอบสร้างเพิ่มด้วย Loading No. แบบเดียวกัน)
-- รันด้วยตัวเองใน SSMS บนฐานทดสอบ ไม่ได้รันโดยระบบหรือ AI  คำเตือน: ข้อมูลที่ลบแล้วกู้คืนไม่ได้
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() NOT LIKE '%[_]dev' AND DB_NAME() NOT LIKE '%test%'
    THROW 50001, 'Refusing to run: this is not a dev/test database.', 1;

BEGIN TRAN;

DELETE FROM [dbo].[ActivityLog] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[TiresCheckerScanStickerLog] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[TiresCheckerScanLog] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[TiresCheckerSticker] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[TiresCheckerPlan] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[TiresChecker] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[PlanTracking] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[DeliveryDetail] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[Delivery] WHERE [LoadingNo] LIKE 'LONKTEST%';
DELETE FROM [dbo].[Plan] WHERE [LoadingNo] LIKE 'LONKTEST%';

COMMIT;

-- ควรได้ 0 แถว
SELECT COUNT(*) AS RemainingMockPlans FROM [dbo].[Plan] WHERE [LoadingNo] LIKE 'LONKTEST%';
