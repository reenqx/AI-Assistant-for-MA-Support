-- ============================================================================
-- 1.MA_Remove_TirecheckerScanLog  (แก้ไขข้อมูลจริง - ลบข้อมูล)
-- ล้างประวัติการสแกนยางและการสแกนสติ๊กเกอร์ ของ Loading No. หนึ่งใบ (ทุก GTCode)
-- ก่อนรัน: เช็คด้วย 01.Query_TiresChecker ว่า Loading No. ถูกต้อง และมี log ที่ต้องการล้างจริง
-- ถ้าต้องการล้างเฉพาะบาง GTCode ให้ใช้สคริปต์ 2.MA_Remove_TirecheckerScanLog(some gtcode) แทน
-- คำเตือน: ข้อมูลที่ลบแล้วกู้คืนไม่ได้
-- ============================================================================

-- Loading No. ที่ต้องการล้าง log (ต้องกรอก ห้ามเว้นว่าง)
DECLARE @LoadingNo VARCHAR(20) = ''


-- TiresCheckerScanLog: ล้าง log การสแกนยางรายเส้นทั้งหมดของ Loading No. นี้
DELETE [TiresCheckerScanLog]
WHERE [LoadingNo] = @LoadingNo

-- TiresCheckerScanStickerLog: ล้าง log การสแกนสติ๊กเกอร์ทั้งหมดของ Loading No. นี้
DELETE [TiresCheckerScanStickerLog]
WHERE [LoadingNo] = @LoadingNo
