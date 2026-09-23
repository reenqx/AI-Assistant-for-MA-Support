-- ============================================================================
-- 2.MA_Remove_TirecheckerScanLog (some gtcode)  (แก้ไขข้อมูลจริง - ลบข้อมูล)
-- ล้างประวัติการสแกนยางและสแกนสติ๊กเกอร์ เฉพาะ GTCode ที่ระบุ ภายใน Loading No. หนึ่งใบ
-- ก่อนรัน: เช็คด้วย 01.Query_TiresChecker ว่า Loading No. และ GTCode ถูกต้อง
-- คำเตือน: ข้อมูลที่ลบแล้วกู้คืนไม่ได้
-- ============================================================================

-- Loading No. และ GTCode ที่ต้องการล้าง log (ต้องกรอกทั้งสองค่า ห้ามเว้นว่าง)
DECLARE @loading VARCHAR(20) = ''
DECLARE @gtcode VARCHAR(20) = ''

-- หมายเหตุเรื่อง DoNo (ปิดไว้เป็นค่าตั้งต้น):
--   ถ้าเป็น market oe ไม่ต้องใส่ Do
--   ถ้าเป็น REP EXPORT ต้องระบุ Do ด้วย ไม่งั้น GTCode นั้นจะถูก cancel จนหมด
--   ถ้าต้องระบุ Do ให้เปิดบรรทัด DECLARE ด้านล่างและบรรทัด AND [DoNo] ในทั้งสองคำสั่ง
--DECLARE @dono VARCHAR(20) = ''


-- TiresCheckerScanLog: ล้าง log การสแกนยางของ GTCode นี้ ใน Loading No. นี้
DELETE [TiresCheckerScanLog]
WHERE [LoadingNo] = @loading
	AND [GTCode] = @gtcode
	--AND [DoNo] = @dono

-- TiresCheckerScanStickerLog: ล้าง log การสแกนสติ๊กเกอร์ของ GTCode นี้ ใน Loading No. นี้
DELETE [TiresCheckerScanStickerLog]
WHERE [LoadingNo] = @loading
	AND [GTCode] = @gtcode
	--AND [DoNo] = @dono
