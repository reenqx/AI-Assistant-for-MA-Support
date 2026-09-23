-- ============================================================================
-- mock-db-verify.sql  (อ่านอย่างเดียว - SELECT ล้วน รันใน SQL Studio / หน้าต่าง Edit ของระบบได้)
-- นับแถวของข้อมูลจำลองแต่ละ Loading เพื่อเทียบกับค่าที่คาดใน README.md (ก่อน-หลังรันสคริปต์แก้ข้อมูล)
-- ============================================================================

SELECT
	 p.[LoadingNo]
	,p.[Market]
	,(SELECT COUNT(*) FROM [dbo].[TiresCheckerPlan] x WHERE x.[LoadingNo] = p.[LoadingNo]) AS 'PlanRows'
	,(SELECT COUNT(*) FROM [dbo].[TiresCheckerScanLog] x WHERE x.[LoadingNo] = p.[LoadingNo]) AS 'ScanLog'
	,(SELECT COUNT(*) FROM [dbo].[TiresCheckerScanLog] x WHERE x.[LoadingNo] = p.[LoadingNo] AND x.[ScanTireFlag] = 1) AS 'ScanLogComplete'
	,(SELECT COUNT(*) FROM [dbo].[TiresCheckerScanStickerLog] x WHERE x.[LoadingNo] = p.[LoadingNo]) AS 'StickerScanLog'
	,(SELECT COUNT(*) FROM [dbo].[TiresCheckerSticker] x WHERE x.[LoadingNo] = p.[LoadingNo]) AS 'StickerRows'
	,(SELECT [CheckerFinishDate] FROM [dbo].[TiresChecker] x WHERE x.[LoadingNo] = p.[LoadingNo]) AS 'CheckerFinishDate'
	,(SELECT [ConfirmLoadingCompleteFlag] FROM [dbo].[TiresChecker] x WHERE x.[LoadingNo] = p.[LoadingNo]) AS 'ConfirmFlag'
	,(SELECT [ActOperation5] FROM [dbo].[PlanTracking] x WHERE x.[LoadingNo] = p.[LoadingNo]) AS 'ActOperation5'
FROM [dbo].[Plan] p
WHERE p.[LoadingNo] LIKE 'LONKTEST%'
ORDER BY p.[LoadingNo]
