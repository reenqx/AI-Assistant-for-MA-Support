-- ============================================================================
-- mock-db-insert.sql  (แก้ไขข้อมูลจริง - เพิ่มข้อมูลจำลองสำหรับทดสอบระบบ)
-- เพิ่มข้อมูลปลอมของ Loading No. ที่ขึ้นต้นด้วย LONKTEST เท่านั้น เพื่อใช้ทดสอบ SQL Studio / สคริปต์ 1-3
-- รันด้วยตัวเองใน SSMS บนฐานทดสอบ (ชื่อลงท้าย _dev หรือมีคำว่า test) ไม่ได้รันโดยระบบหรือ AI
-- ล้างข้อมูลชุดนี้ด้วย mock-db-cleanup.sql  ตรวจผลด้วย mock-db-verify.sql
-- ============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

-- กันรันผิดฐาน: ปฏิเสธถ้าไม่ใช่ฐานทดสอบ
IF DB_NAME() NOT LIKE '%[_]dev' AND DB_NAME() NOT LIKE '%test%'
    THROW 50001, 'Refusing to run: this is not a dev/test database.', 1;

-- กันรันซ้ำ: ต้องล้างของเดิมก่อน
IF EXISTS (SELECT 1 FROM [dbo].[Plan] WHERE [LoadingNo] LIKE 'LONKTEST%')
    THROW 50002, 'Mock data already present. Run mock-db-cleanup.sql first.', 1;

BEGIN TRAN;

-- ---------------------------------------------------------------- LONKTEST0001: สแกนครบแล้ว + จบ Checker แล้ว (ใช้ทดสอบสคริปต์ 3 reset สถานะจบ)
INSERT INTO [dbo].[Plan] ([LoadingNo], [Market], [Warehouse], [PlanDate], [LoadingStatus], [CreateDate], [CreateBy])
VALUES ('LONKTEST0001', 'OE', 'WH-TEST', '2026-09-15 00:00:00', 'Active', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[PlanTracking] ([LoadingNo], [ActOperation4], [ActOperation4By], [ActOperation5], [ActOperation5By], [CreateDate], [CreateBy])
VALUES ('LONKTEST0001', '2026-09-15 08:00:00', 'tester', '2026-09-15 10:30:00', 'tester', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresChecker] ([LoadingNo], [Market], [CheckerStartDate], [CheckerFinishDate], [ConfirmLoadingCompleteFlag], [ConfirmLoadingCompleteDate], [ConfirmLoadingCompleteBy], [CreateDate], [CreateBy])
VALUES ('LONKTEST0001', 'OE', '2026-09-15 08:00:00', '2026-09-15 10:30:00', 1, '2026-09-15 10:35:00', 'tester', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerPlan] ([LoadingNo], [DONo], [GTCode], [LoadingQTY], [CreateDate], [CreateBy])
VALUES
    ('LONKTEST0001', 'DOTEST0001', 'GTTEST001', 4, '2026-09-14 08:00:00', 'tester'),
    ('LONKTEST0001', 'DOTEST0001', 'GTTEST002', 2, '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerSticker] ([LoadingNo], [GTCode], [StickerCode], [IsUsed], [StickerPlanQTY], [StickerPrepareQTY], [CreateDate], [CreateBy])
VALUES
    ('LONKTEST0001', 'GTTEST001', 'STKT0001A', 1, 4, 4, '2026-09-14 09:00:00', 'tester'),
    ('LONKTEST0001', 'GTTEST002', 'STKT0001B', 1, 2, 2, '2026-09-14 09:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerScanLog] ([LoadingNo], [DONo], [Warehouse], [SerialNo], [GTCode], [SizeCode], [ScanTireFlag], [ErrorCode], [ScanDate], [ScanBy], [Gate])
VALUES
    ('LONKTEST0001', 'DOTEST0001', 'WH-TEST', 'SNT0001-01', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-15 08:10:00', 'tester', 'GATE-1'),
    ('LONKTEST0001', 'DOTEST0001', 'WH-TEST', 'SNT0001-02', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-15 08:11:00', 'tester', 'GATE-1'),
    ('LONKTEST0001', 'DOTEST0001', 'WH-TEST', 'SNT0001-03', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-15 08:12:00', 'tester', 'GATE-1'),
    ('LONKTEST0001', 'DOTEST0001', 'WH-TEST', 'SNT0001-04', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-15 08:13:00', 'tester', 'GATE-1'),
    ('LONKTEST0001', 'DOTEST0001', 'WH-TEST', 'SNT0001-05', 'GTTEST002', 'SZ-002', 1, NULL, '2026-09-15 08:14:00', 'tester', 'GATE-1'),
    ('LONKTEST0001', 'DOTEST0001', 'WH-TEST', 'SNT0001-06', 'GTTEST002', 'SZ-002', 1, NULL, '2026-09-15 08:15:00', 'tester', 'GATE-1');
INSERT INTO [dbo].[TiresCheckerScanStickerLog] ([LoadingNo], [DONo], [SerialNo], [GTCode], [SizeCode], [StickerBarcode], [ScanDate])
VALUES
    ('LONKTEST0001', 'DOTEST0001', 'SNT0001-01', 'GTTEST001', 'SZ-001', 'BCSNT000101', '2026-09-15 08:20:00'),
    ('LONKTEST0001', 'DOTEST0001', 'SNT0001-02', 'GTTEST001', 'SZ-001', 'BCSNT000102', '2026-09-15 08:20:00'),
    ('LONKTEST0001', 'DOTEST0001', 'SNT0001-03', 'GTTEST001', 'SZ-001', 'BCSNT000103', '2026-09-15 08:20:00'),
    ('LONKTEST0001', 'DOTEST0001', 'SNT0001-04', 'GTTEST001', 'SZ-001', 'BCSNT000104', '2026-09-15 08:20:00'),
    ('LONKTEST0001', 'DOTEST0001', 'SNT0001-05', 'GTTEST002', 'SZ-002', 'BCSNT000105', '2026-09-15 08:20:00'),
    ('LONKTEST0001', 'DOTEST0001', 'SNT0001-06', 'GTTEST002', 'SZ-002', 'BCSNT000106', '2026-09-15 08:20:00');
INSERT INTO [dbo].[Delivery] ([DONo], [LoadingNo], [CustomerCode], [CustomerName], [CreateDate], [CreateBy])
VALUES ('DOTEST0001', 'LONKTEST0001', 'CUSTTEST01', 'Test Customer Co., Ltd.', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[DeliveryDetail] ([DONo], [LoadingNo], [SaleCode], [GTCode], [SpecCode], [SizeName], [TranQty], [CreateDate], [CreateBy])
VALUES
    ('DOTEST0001', 'LONKTEST0001', 'SALE1', 'GTTEST001', 'SPEC01', '205/55R16 TEST', 4, '2026-09-14 08:00:00', 'tester'),
    ('DOTEST0001', 'LONKTEST0001', 'SALE2', 'GTTEST002', 'SPEC01', '205/55R16 TEST', 2, '2026-09-14 08:00:00', 'tester');

-- ---------------------------------------------------------------- LONKTEST0002: สแกนไม่ครบ มี error บางเส้น (ใช้ทดสอบสคริปต์ 1 ลบ scan log ทั้ง Loading)
INSERT INTO [dbo].[Plan] ([LoadingNo], [Market], [Warehouse], [PlanDate], [LoadingStatus], [CreateDate], [CreateBy])
VALUES ('LONKTEST0002', 'OE', 'WH-TEST', '2026-09-16 00:00:00', 'Active', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[PlanTracking] ([LoadingNo], [ActOperation4], [ActOperation4By], [ActOperation5], [ActOperation5By], [CreateDate], [CreateBy])
VALUES ('LONKTEST0002', '2026-09-16 08:00:00', 'tester', NULL, NULL, '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresChecker] ([LoadingNo], [Market], [CheckerStartDate], [CheckerFinishDate], [ConfirmLoadingCompleteFlag], [ConfirmLoadingCompleteDate], [ConfirmLoadingCompleteBy], [CreateDate], [CreateBy])
VALUES ('LONKTEST0002', 'OE', '2026-09-16 08:00:00', NULL, NULL, NULL, NULL, '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerPlan] ([LoadingNo], [DONo], [GTCode], [LoadingQTY], [CreateDate], [CreateBy])
VALUES
    ('LONKTEST0002', 'DOTEST0002', 'GTTEST001', 4, '2026-09-14 08:00:00', 'tester'),
    ('LONKTEST0002', 'DOTEST0002', 'GTTEST003', 3, '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerSticker] ([LoadingNo], [GTCode], [StickerCode], [IsUsed], [StickerPlanQTY], [StickerPrepareQTY], [CreateDate], [CreateBy])
VALUES
    ('LONKTEST0002', 'GTTEST001', 'STKT0002A', 1, 4, 4, '2026-09-14 09:00:00', 'tester'),
    ('LONKTEST0002', 'GTTEST003', 'STKT0002B', 1, 3, 3, '2026-09-14 09:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerScanLog] ([LoadingNo], [DONo], [Warehouse], [SerialNo], [GTCode], [SizeCode], [ScanTireFlag], [ErrorCode], [ScanDate], [ScanBy], [Gate])
VALUES
    ('LONKTEST0002', 'DOTEST0002', 'WH-TEST', 'SNT0002-01', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-16 08:10:00', 'tester', 'GATE-1'),
    ('LONKTEST0002', 'DOTEST0002', 'WH-TEST', 'SNT0002-02', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-16 08:11:00', 'tester', 'GATE-1'),
    ('LONKTEST0002', 'DOTEST0002', 'WH-TEST', 'SNT0002-03', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-16 08:12:00', 'tester', 'GATE-1'),
    ('LONKTEST0002', 'DOTEST0002', 'WH-TEST', 'SNT0002-04', 'GTTEST003', 'SZ-003', 0, 'E001', '2026-09-16 08:13:00', 'tester', 'GATE-1'),
    ('LONKTEST0002', 'DOTEST0002', 'WH-TEST', 'SNT0002-05', 'GTTEST003', 'SZ-003', 0, 'E002', '2026-09-16 08:14:00', 'tester', 'GATE-1');
INSERT INTO [dbo].[TiresCheckerScanStickerLog] ([LoadingNo], [DONo], [SerialNo], [GTCode], [SizeCode], [StickerBarcode], [ScanDate])
VALUES
    ('LONKTEST0002', 'DOTEST0002', 'SNT0002-01', 'GTTEST001', 'SZ-001', 'BCSNT000201', '2026-09-15 08:20:00'),
    ('LONKTEST0002', 'DOTEST0002', 'SNT0002-02', 'GTTEST001', 'SZ-001', 'BCSNT000202', '2026-09-15 08:20:00'),
    ('LONKTEST0002', 'DOTEST0002', 'SNT0002-03', 'GTTEST001', 'SZ-001', 'BCSNT000203', '2026-09-15 08:20:00');

-- ---------------------------------------------------------------- LONKTEST0003: ตลาด REP (ต้องระบุ DoNo) 2 GTCode (ใช้ทดสอบสคริปต์ 2 ลบเฉพาะบาง GTCode)
INSERT INTO [dbo].[Plan] ([LoadingNo], [Market], [Warehouse], [PlanDate], [LoadingStatus], [CreateDate], [CreateBy])
VALUES ('LONKTEST0003', 'REP', 'WH-TEST', '2026-09-17 00:00:00', 'Active', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[PlanTracking] ([LoadingNo], [ActOperation4], [ActOperation4By], [ActOperation5], [ActOperation5By], [CreateDate], [CreateBy])
VALUES ('LONKTEST0003', '2026-09-17 09:00:00', 'tester', NULL, NULL, '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresChecker] ([LoadingNo], [Market], [CheckerStartDate], [CheckerFinishDate], [ConfirmLoadingCompleteFlag], [ConfirmLoadingCompleteDate], [ConfirmLoadingCompleteBy], [CreateDate], [CreateBy])
VALUES ('LONKTEST0003', 'REP', '2026-09-17 09:00:00', NULL, NULL, NULL, NULL, '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerPlan] ([LoadingNo], [DONo], [GTCode], [LoadingQTY], [CreateDate], [CreateBy])
VALUES
    ('LONKTEST0003', 'DOTEST0003', 'GTTEST001', 3, '2026-09-14 08:00:00', 'tester'),
    ('LONKTEST0003', 'DOTEST0003', 'GTTEST002', 3, '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerSticker] ([LoadingNo], [GTCode], [StickerCode], [IsUsed], [StickerPlanQTY], [StickerPrepareQTY], [CreateDate], [CreateBy])
VALUES
    ('LONKTEST0003', 'GTTEST001', 'STKT0003A', 1, 3, 3, '2026-09-14 09:00:00', 'tester'),
    ('LONKTEST0003', 'GTTEST002', 'STKT0003B', 1, 3, 3, '2026-09-14 09:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerScanLog] ([LoadingNo], [DONo], [Warehouse], [SerialNo], [GTCode], [SizeCode], [ScanTireFlag], [ErrorCode], [ScanDate], [ScanBy], [Gate])
VALUES
    ('LONKTEST0003', 'DOTEST0003', 'WH-TEST', 'SNT0003-01', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-17 08:10:00', 'tester', 'GATE-1'),
    ('LONKTEST0003', 'DOTEST0003', 'WH-TEST', 'SNT0003-02', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-17 08:11:00', 'tester', 'GATE-1'),
    ('LONKTEST0003', 'DOTEST0003', 'WH-TEST', 'SNT0003-03', 'GTTEST001', 'SZ-001', 1, NULL, '2026-09-17 08:12:00', 'tester', 'GATE-1'),
    ('LONKTEST0003', 'DOTEST0003', 'WH-TEST', 'SNT0003-04', 'GTTEST002', 'SZ-002', 1, NULL, '2026-09-17 08:13:00', 'tester', 'GATE-1'),
    ('LONKTEST0003', 'DOTEST0003', 'WH-TEST', 'SNT0003-05', 'GTTEST002', 'SZ-002', 1, NULL, '2026-09-17 08:14:00', 'tester', 'GATE-1'),
    ('LONKTEST0003', 'DOTEST0003', 'WH-TEST', 'SNT0003-06', 'GTTEST002', 'SZ-002', 1, NULL, '2026-09-17 08:15:00', 'tester', 'GATE-1');
INSERT INTO [dbo].[TiresCheckerScanStickerLog] ([LoadingNo], [DONo], [SerialNo], [GTCode], [SizeCode], [StickerBarcode], [ScanDate])
VALUES
    ('LONKTEST0003', 'DOTEST0003', 'SNT0003-01', 'GTTEST001', 'SZ-001', 'BCSNT000301', '2026-09-15 08:20:00'),
    ('LONKTEST0003', 'DOTEST0003', 'SNT0003-02', 'GTTEST001', 'SZ-001', 'BCSNT000302', '2026-09-15 08:20:00'),
    ('LONKTEST0003', 'DOTEST0003', 'SNT0003-03', 'GTTEST001', 'SZ-001', 'BCSNT000303', '2026-09-15 08:20:00'),
    ('LONKTEST0003', 'DOTEST0003', 'SNT0003-04', 'GTTEST002', 'SZ-002', 'BCSNT000304', '2026-09-15 08:20:00'),
    ('LONKTEST0003', 'DOTEST0003', 'SNT0003-05', 'GTTEST002', 'SZ-002', 'BCSNT000305', '2026-09-15 08:20:00'),
    ('LONKTEST0003', 'DOTEST0003', 'SNT0003-06', 'GTTEST002', 'SZ-002', 'BCSNT000306', '2026-09-15 08:20:00');
INSERT INTO [dbo].[Delivery] ([DONo], [LoadingNo], [CustomerCode], [CustomerName], [CreateDate], [CreateBy])
VALUES ('DOTEST0003', 'LONKTEST0003', 'CUSTTEST02', 'REP Test Customer Co., Ltd.', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[DeliveryDetail] ([DONo], [LoadingNo], [SaleCode], [GTCode], [SpecCode], [SizeName], [TranQty], [CreateDate], [CreateBy])
VALUES
    ('DOTEST0003', 'LONKTEST0003', 'SALE1', 'GTTEST001', 'SPEC01', '205/55R16 TEST', 3, '2026-09-14 08:00:00', 'tester'),
    ('DOTEST0003', 'LONKTEST0003', 'SALE2', 'GTTEST002', 'SPEC01', '205/55R16 TEST', 3, '2026-09-14 08:00:00', 'tester');

-- ---------------------------------------------------------------- LONKTEST0004: มีแผนแต่ยังไม่มี scan log เลย (ทดสอบกรณี 0 แถว / ไม่มีอะไรให้ล้าง)
INSERT INTO [dbo].[Plan] ([LoadingNo], [Market], [Warehouse], [PlanDate], [LoadingStatus], [CreateDate], [CreateBy])
VALUES ('LONKTEST0004', 'OE', 'WH-TEST', '2026-09-18 00:00:00', 'Active', '2026-09-14 08:00:00', 'tester');
INSERT INTO [dbo].[TiresCheckerPlan] ([LoadingNo], [DONo], [GTCode], [LoadingQTY], [CreateDate], [CreateBy])
VALUES
    ('LONKTEST0004', 'DOTEST0004', 'GTTEST002', 5, '2026-09-14 08:00:00', 'tester');

-- ActivityLog ตัวอย่างของ LONKTEST0001 (LogID เป็น identity ระบบกำหนดเอง)
INSERT INTO [dbo].[ActivityLog] ([Username], [ActivityDate], [ScreenCode], [EventLog], [SubEvent], [LoadingNo], [Note], [ApplicationName])
VALUES
    ('tester', '2026-09-15 08:00:00', 'TCK001', 'Start', 'Checker', 'LONKTEST0001', 'mock: start checker', 'MOCK'),
    ('tester', '2026-09-15 10:30:00', 'TCK001', 'Finish', 'Checker', 'LONKTEST0001', 'mock: finish checker', 'MOCK'),
    ('tester', '2026-09-15 10:35:00', 'TCK002', 'Confirm', 'LoadingComplete', 'LONKTEST0001', 'mock: confirm loading complete', 'MOCK');

COMMIT;

-- สรุปที่เพิ่มเข้าไป
SELECT [LoadingNo], [Market] FROM [dbo].[Plan] WHERE [LoadingNo] LIKE 'LONKTEST%' ORDER BY [LoadingNo];
