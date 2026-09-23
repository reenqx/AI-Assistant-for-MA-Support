-- ============================================================================
-- 01.Query_TiresChecker  (อ่านอย่างเดียว - SELECT ล้วน)
-- ตรวจสถานะ Tires Checker ของ Loading No. หนึ่งใบ ตั้งแต่แผน การสแกนยาง สติ๊กเกอร์
-- การจัดเส้นทาง (Routing) จนถึงค่าขนส่ง ก่อนตัดสินใจ Reset หรือแก้ข้อมูล
-- วิธีใช้: ใส่ค่า @LoadingNo ให้ครบก่อนรัน
-- ============================================================================

-- Loading No. ที่ต้องการตรวจ (ต้องกรอก ห้ามเว้นว่าง)
DECLARE @LoadingNo VARCHAR(20) = ''


-- ----------------------------------------------------------------------------
-- สรุปสถานะรวมของ Loading No. (ส่วนที่ 1)
-- ----------------------------------------------------------------------------
SELECT
	 [LoadingNo]
	,[Market]
	,CONVERT( DATE, [PlanDate] ) AS 'PlanDate'

	-- TiresCheckerPlan: จำนวนยางทั้งหมดตามแผนที่ต้องโหลด (รวม LoadingQTY)
	,( SELECT CONVERT( INT, SUM([LoadingQTY]) ) FROM [MoCS].[dbo].[TiresCheckerPlan] WHERE [LoadingNo] = @LoadingNo ) AS 'TotalQty'

	-- TiresCheckerScanLog: จำนวนยางที่มี log การสแกนทั้งหมด
	,( SELECT COUNT(*) FROM [MoCS].[dbo].[TiresCheckerScanLog] WHERE [LoadingNo] = @LoadingNo ) AS 'TotalTiresScanLog'

	-- TiresCheckerScanLog: จำนวนยางที่สแกนสำเร็จแล้ว (ScanTireFlag = 1)
	,( SELECT COUNT(*) FROM [MoCS].[dbo].[TiresCheckerScanLog] WHERE [LoadingNo] = @LoadingNo AND [ScanTireFlag] = 1 ) AS 'CompleteScanLog'

	-- TiresCheckerSticker: จำนวนสติ๊กเกอร์ที่เตรียมไว้และถูกใช้แล้ว (IsUsed = 1)
	,( SELECT CONVERT( INT, SUM([StickerPrepareQTY]) ) FROM [MoCS].[dbo].[TiresCheckerSticker] WHERE [LoadingNo] = @LoadingNo AND [IsUsed] = 1 ) AS 'TotalSticker'

	-- TiresCheckerScanStickerLog: จำนวนสติ๊กเกอร์ที่ถูกสแกนแล้ว
	,( SELECT COUNT(*) FROM [MoCS].[dbo].[TiresCheckerScanStickerLog] WHERE [LoadingNo] = @LoadingNo ) AS 'StickerScanLog'

	-- PlanTracking: วันที่เริ่ม Checker (ActOperation4) และวันที่จบ Checker (ActOperation5)
	,( SELECT [ActOperation4] FROM [MoCS].[dbo].[PlanTracking] WHERE [LoadingNo] = @LoadingNo ) AS 'CheckerStartDate'
	,( SELECT [ActOperation5] FROM [MoCS].[dbo].[PlanTracking] WHERE [LoadingNo] = @LoadingNo ) AS 'CheckerFinishDate'

	-- RoutingJob: Loading No. นี้ถูกนำไปจัดเส้นทาง (Routing) แล้วหรือยัง
	,( SELECT CASE WHEN EXISTS ( SELECT * FROM [MoCS].[mobile].[RoutingJob] WHERE [LoadingNo] = @LoadingNo ) THEN 'True' ELSE 'False' END ) AS 'IsRoutingJob'

	-- Routing: วันที่สร้างเส้นทางที่ผูกกับ Loading No. นี้ (ผ่าน RoutingJob)
	,( SELECT [CreateDate] FROM [MoCS].[mobile].[Routing] WHERE [RoutingID] IN (SELECT [RoutingID] FROM [MoCS].[mobile].[RoutingJob] WHERE [LoadingNo] = @LoadingNo) ) AS 'RoutingCreateDate'

	-- FreightChargeDetail: Loading No. นี้ถูกคิดค่าขนส่งแล้วหรือยัง
	,( SELECT CASE WHEN EXISTS ( SELECT * FROM [MoCS].[fcs].[FreightChargeDetail] WHERE [LoadingNo] = @LoadingNo ) THEN 'True' ELSE 'False' END ) AS 'IsFreightCharge'
FROM [MoCS].[dbo].[Plan]
WHERE [LoadingNo] = @LoadingNo


-- ----------------------------------------------------------------------------
-- ส่วนที่ 2: ข้อมูลดิบรายตาราง (แต่ละตารางขึ้นเป็นผลลัพธ์แยกกัน)
-- ----------------------------------------------------------------------------

-- Plan: แผนหลักของ Loading No. (ตลาด วันที่แผน)
SELECT 'Plan' AS 'Plan', * FROM [MoCS].[dbo].[Plan] WHERE [LoadingNo] = @LoadingNo

-- Delivery: ข้อมูลการส่งสินค้าของ Loading No.
SELECT 'Delivery' AS 'Delivery', * FROM [MoCS].[dbo].[Delivery] WHERE [LoadingNo] = @LoadingNo

-- DeliveryDetail: รายละเอียดรายการในการส่งสินค้า
SELECT 'DeliveryDetail' AS 'DeliveryDetail', * FROM [MoCS].[dbo].[DeliveryDetail] WHERE [LoadingNo] = @LoadingNo

-- PlanTracking: ติดตามสถานะขั้นตอนของแผน (ดูวันเริ่ม/จบ Checker ที่ ActOperation4 และ ActOperation5)
SELECT 'PlanTracking' AS 'PlanTracking', * FROM [MoCS].[dbo].[PlanTracking] WHERE [LoadingNo] = @LoadingNo

-- TiresCheckerPlan: จำนวนยางที่ต้องเช็คตามแผน (LoadingQTY)
SELECT 'TiresCheckerPlan' AS 'TiresCheckerPlan', * FROM [MoCS].[dbo].[TiresCheckerPlan] WHERE [LoadingNo] = @LoadingNo

-- TiresChecker: สถานะการเช็คของ Loading No. (วันที่ Checker จบ และการยืนยันโหลดเสร็จ)
SELECT 'TiresChecker' AS 'TiresChecker', * FROM [MoCS].[dbo].[TiresChecker] WHERE [LoadingNo] = @LoadingNo

-- TiresCheckerSticker: สติ๊กเกอร์ที่เตรียมไว้ (จำนวน และสถานะถูกใช้แล้วหรือไม่)
SELECT 'TiresCheckerSticker' AS 'TiresCheckerSticker', * FROM [MoCS].[dbo].[TiresCheckerSticker] WHERE [LoadingNo] = @LoadingNo

-- TiresCheckerScanLog: log การสแกนยางรายเส้น (GTCode, SerialNo, DoNo และสถานะสแกนสำเร็จ)
SELECT 'TiresCheckerScanLog' AS 'TiresCheckerScanLog', * FROM [MoCS].[dbo].[TiresCheckerScanLog] WHERE [LoadingNo] = @LoadingNo

-- TiresCheckerScanStickerLog: log การสแกนสติ๊กเกอร์ (GTCode, SerialNo, DoNo)
SELECT 'TiresCheckerScanStickerLog' AS 'TiresCheckerScanStickerLog', * FROM [MoCS].[dbo].[TiresCheckerScanStickerLog] WHERE [LoadingNo] = @LoadingNo

-- Routing: เส้นทางที่ผูกกับ Loading No. นี้ (หาจาก RoutingJob)
SELECT 'Routing' AS 'Routing', * FROM [MoCS].[mobile].[Routing] WHERE [RoutingID] IN (SELECT [RoutingID] FROM [MoCS].[mobile].[RoutingJob] WHERE [LoadingNo] = @LoadingNo)

-- RoutingJob: งาน Routing ที่มี Loading No. นี้
SELECT 'RoutingJob' AS 'RoutingJob', * FROM [MoCS].[mobile].[RoutingJob] WHERE [LoadingNo] = @LoadingNo

-- ActivityLog: ประวัติกิจกรรมของ Loading No. เรียงตาม LogID (เก่าไปใหม่)
SELECT 'ActivityLog' AS 'ActivityLog', * FROM [MoCS].[dbo].[ActivityLog] WHERE [LoadingNo] = @LoadingNo ORDER BY [LogID]

-- WmsLog: log การเชื่อมต่อกับระบบ WMS เรียงตามเวลาที่บันทึก (เก่าไปใหม่)
SELECT 'WMSLog' AS 'WMSLog', * FROM [MoCS].[logs].[WmsLog] WHERE [LoadingNo] = @LoadingNo ORDER BY [CreatedAt]

-- EstimateCost: ประมาณการค่าใช้จ่ายที่ผูกกับ Loading No. นี้ (หาจาก EstimateCostDetail)
SELECT 'EstimateCost' AS 'EstimateCost', * FROM [MoCS].[fcs].[EstimateCost] WHERE [EstimateCostId] IN (SELECT [EstimateCostId] FROM [MoCS].[fcs].[EstimateCostDetail] WHERE [LoadingNo] = @LoadingNo)

-- EstimateCostDetail: รายละเอียดประมาณการค่าใช้จ่ายของ Loading No. นี้
SELECT 'EstimateCostDetail' AS 'EstimateCostDetail', * FROM [MoCS].[fcs].[EstimateCostDetail] WHERE [LoadingNo] = @LoadingNo
