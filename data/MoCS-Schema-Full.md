# MoCS Database Schema (full, generated)

สร้างอัตโนมัติจากเมทาดาทาของฐานข้อมูล `MoCS_dev` (INFORMATION_SCHEMA + sys catalog) เมื่อ 2026-09-21 — ไม่มีข้อมูลในตารางปนอยู่ ไฟล์นี้ **ไม่ถูกส่งเข้า prompt ทั้งไฟล์** ระบบเลือกเฉพาะตารางที่เกี่ยวข้องกับคำถามให้ (ดู `FileDbSchemaProvider`) ส่วนคำอธิบายความหมายของคอลัมน์และกติกาการอ้างชื่อ อยู่ใน `MoCS-Schema-Reference.md`

รูปแบบต่อ 1 ตาราง: หัวข้อ `### schema.ชื่อ (TABLE|VIEW)` ตามด้วย `PK:` (ถ้ามี), `Columns:` (คอลัมน์ที่ไม่ระบุ null = NOT NULL), `FK:` (ถ้ามี) — ถ้าตารางในฐานเปลี่ยน ให้สร้างไฟล์นี้ใหม่ด้วย `tools/Export-DbSchema.ps1`

### dbo.ActivityLog (TABLE)
PK: LogID
Columns: LogID int, Username varchar(20), ActivityDate datetime2 null, ScreenCode varchar(20) null, EventLog varchar(50) null, SubEvent varchar(50) null, LoadingNo varchar(20) null, DONo varchar(20) null, SerialNo varchar(20) null, Note varchar(255) null, IPAdress varchar(max) null, Warehouse varchar(20) null, ApplicationName varchar(50) null

### dbo.AreaLeadtime (TABLE)
Columns: AreaCode varchar(20) null, PeriodTypeCode varchar(20) null, Leadtime int null

### dbo.Assembly (TABLE)
PK: PickingNo, Warehouse
Columns: PickingNo varchar(20), Warehouse varchar(20), AssemblyStartDate datetime2 null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.AssemblyLog (TABLE)
PK: LogID
Columns: LogID bigint, PickingNo varchar(20), SerialNo varchar(20) null, JigCode varchar(20) null, GTCodeBlueprint varchar(20) null, GTCode varchar(20) null, SizeCode varchar(20) null, ScanFlag int, ErrorCode varchar(50) null, ErrorDate datetime2 null, AssemblyDate datetime2, AssemblyBy varchar(20), UnlockDate datetime2 null, UnlockBy varchar(20) null

### dbo.AssemblyStock (TABLE)
PK: GTCode, Warehouse
Columns: GTCode varchar(20), Warehouse varchar(20), SizeCode varchar(20) null, JigType varchar(20) null, QTY int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.AssemblyStockDetail (TABLE)
PK: AssemblyStockDetailNo
Columns: AssemblyStockDetailNo bigint, PickingNo varchar(20) null, GTCode varchar(20), Warehouse varchar(20), SerialBarcode varchar(20), JigCode varchar(20) null, ScanedFlag int null, AssemblyDate datetime2, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.BsHoliday (TABLE)
PK: HolidayDate
Columns: HolidayDate date, Holiday varchar(100), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.ClaimDealer (TABLE)
Columns: Dealer Code nvarchar(255) null, BS Code nvarchar(255) null, Dealer Name nvarchar(255) null, Address nvarchar(255) null, Tel nvarchar(255) null, Dealer Type nvarchar(255) null, Province nvarchar(255) null, Region nvarchar(255) null

### dbo.Config (TABLE)
PK: KeyName
Columns: KeyName varchar(100), Value varchar(150) null, Description varchar(150) null, ModuleUsage varchar(200) null

### dbo.ConfigDocsReceive (TABLE)
PK: Id
Columns: Id int, DocsReceiveTypeId int, BarcodeX float, BarcodeY float, BarcodeWidth float, BarcodeHeight float, BarcodeResizeWidth int, SignatureX float null, SignatureY float null, SignatureWidth float null, SignatureHeight float null, SignatureThreshold float null, IsEnabled bit
FK: DocsReceiveTypeId -> fcs.DocsReceiveType.Id

### dbo.ConfigTireChecker (TABLE)
PK: Warehouse
Columns: Warehouse varchar(20), Description nvarchar(250) null, EnableTirechecker bit, EnableWMS bit, InspectorReportName nvarchar(250) null, CopiesPrintedAmount int, StartScanLoadingUrl nvarchar(250) null, ConfirmLoadingCompleteUrl nvarchar(250) null

### dbo.ConvertPending (TABLE)
PK: ProcessId
Columns: ProcessId int, ProcessDate datetime2 null, ProcessBy varchar(20) null

### dbo.ConvertPendingDetail (TABLE)
PK: DetailId
Columns: DetailId int, ProcessId int null, LoadingNo varchar(20) null, PlanDate datetime2 null, PickingStartTime datetime2 null, PickingFinishTime datetime2 null, TruckArrivedTime datetime2 null, LoadingStartTime datetime2 null, LoadingFinishTime datetime2 null, DeliveryTime datetime2 null, TimespanForTruckArrived int null, TimespanForLoading int null, TransportCompany varchar(50) null, TruckSize varchar(20) null, LicensePlate varchar(100) null, DoNo varchar(20) null, TransferNo varchar(20) null, PlanDeliveryDate datetime2 null, CustomerCode varchar(20) null, InvoiceNo varchar(20) null, Market varchar(20) null, Warehouse varchar(20) null, GtCode varchar(20) null, SaleCode varchar(20) null, LoadQty numeric(18,4) null, BatchNo varchar(20) null, Grade varchar(20) null, ProductionDate datetime2 null, SkipFifoDotFlag int null, GtCodeNew varchar(20) null, ProcessDate datetime2 null, ProcessBy varchar(20) null
FK: ProcessId -> dbo.ConvertPending.ProcessId

### dbo.Customer (TABLE)
PK: CustomerCode
Columns: CustomerCode varchar(20), CustomerName varchar(150) null, CustomerGroupCode varchar(20) null, Address varchar(255) null, ProvinceCode int null, Email varchar(150) null, Mobile varchar(150) null, DeliveryLeadTime int null, TiresAgeAcceptance int null, Market varchar(20) null, ShipmentType2D int null, Latitude float null, Longitude float null, FcsDestination int null, MultipleDestinationFlag int null, TimespanOe int null, RequireSprFlag int null, RoutingType varchar(20) null, Note varchar(max) null, ZoneID int null, CustomerLocationFlag int null, CustomerType varchar(20) null, Postcode varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.CustomerDayoff (TABLE)
PK: CustomerCode, Day
Columns: CustomerCode varchar(20), Day varchar(20), Dayofweek int, Closed int, SectionFlag int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.CustomerDistance (TABLE)
PK: CustomerCode, WarehouseCode
Columns: CustomerCode varchar(20), WarehouseCode varchar(20), Distance numeric(18,4) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.CustomerZone (TABLE)
PK: CustomerCode, Warehouse
Columns: CustomerCode varchar(20), Warehouse varchar(20), ZoneID int, ZoneIDLv2 int

### dbo.DashboardTransportation (TABLE)
PK: TransportationCode
Columns: TransportationCode varchar(100), TransportationName varchar(100) null, Color1 varchar(20) null, Color2 varchar(20) null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DashGiveAwayRate (TABLE)
Columns: GtCode varchar(20), DestinationId int, ChargeRate decimal(18,2) null

### dbo.DelayTime (TABLE)
PK: ReasonId
Columns: ReasonId int, ReasonCode nvarchar(20) null, ReasonName nvarchar(255) null, DelayTime int null, IsActive bit null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DeleteDelivery (TABLE)
Columns: DONo varchar(20), LoadingNo varchar(20), InvoiceNo varchar(20) null, DONoFile varchar(20) null, SuffixNo varchar(20) null, CustomerCode varchar(20), CustomerName varchar(150) null, ShipTo varchar(255) null, Province varchar(20) null, BF varchar(20) null, PlanDeliveryDate datetime2 null, ChangeDestination int null, ChangeDestinationBy varchar(20) null, ChangeDestinationDate datetime2 null, OTP varchar(10) null, ReferenceNo varchar(10) null, ApprovedDate datetime2 null, ETA datetime2 null, DOReceiveStatus varchar(20) null, ReceiveDate datetime2 null, DelayReason varchar(150) null, CompleteStatus varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DeleteDeliveryDetailLog (TABLE)
Columns: DONo varchar(20), LoadingNo varchar(20), SaleCode varchar(20), GTCode varchar(20), SpecCode varchar(10), SizeName varchar(50), Grade varchar(20) null, DocnoNo varchar(20) null, DoLine varchar(20) null, BSJCode varchar(20) null, TranQty numeric(18,4), ProductionDate datetime2 null, LoadedQty numeric(18,4) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeleteDeliveryLog (TABLE)
Columns: DONo varchar(20), LoadingNo varchar(20), InvoiceNo varchar(20) null, DONoFile varchar(20) null, SuffixNo varchar(20) null, CustomerCode varchar(20), CustomerName varchar(150) null, ShipTo varchar(255) null, Province varchar(20) null, BF varchar(20) null, PlanDeliveryDate datetime2 null, ChangeDestination int null, ChangeDestinationBy varchar(20) null, ChangeDestinationDate datetime2 null, OTP varchar(10) null, ReferenceNo varchar(10) null, ApprovedDate datetime2 null, ETA datetime2 null, PendingFlag int null, PendingDate datetime2 null, ResendFlag int null, ResendDate datetime2 null, DistributionCenterFlag int null, DistributorCode varchar(50) null, ArrivedAtDistributionCenterBy varchar(20) null, ArrivedAtDistributionCenterDate datetime2 null, StartFromDistributionCenterBy varchar(20) null, StartFromDistributionCenterDate datetime2 null, SendStatus varchar(20) null, SendDate datetime2 null, SendBy varchar(20) null, SendLatitude numeric(12,9) null, SendLongtitude numeric(12,9) null, ImportDistributorStatusId int null, ImportDOStatusId int null, DOReceiveStatus varchar(20) null, ReceiveDate datetime2 null, DelayReasonId int null, DelayReason varchar(150) null, ImportDelayReasonID int null, CompleteStatus varchar(20) null, DOReceiveBy varchar(20) null, ReceiveLatitude float null, ReceiveLongtitude float null, LatestProofType varchar(20) null, LatestAction varchar(20) null, GPSStatus int null, ETATruckDepart datetime2 null, ETADistributorDepart datetime2 null, DelayFlag int null, DelayTime int null, DistributorDelayTime int null, ETAFinal datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeleteDeliveryRejectReasonLog (TABLE)
Columns: DONo varchar(20), LoadingNo varchar(20), ReasonCode varchar(20), Note nvarchar(150) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeleteDeliverySerialLog (TABLE)
Columns: DONo varchar(20), LoadingNo varchar(20), RecNo int, SerialNo varchar(20) null, SerialReceiveStatus varchar(20) null, ScanDate datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeletePlanLog (TABLE)
Columns: LoadingNo varchar(20), ShippingNo varchar(20) null, Market varchar(20) null, Warehouse varchar(20) null, MasterWarehouse varchar(20) null, WhNo varchar(20) null, Plant varchar(20) null, Sloc varchar(20) null, SubmitPlanTiresChecker int null, LoadingStatus varchar(20) null, PlanCreateDate datetime2 null, LoadFinishDate datetime2 null, FileName varchar(20) null, PlanDate datetime2 null, TruckNote varchar(50) null, TruckSize varchar(100) null, LoadRemark varchar(100) null, ExportShippingAgentFlag int null, BatchNo varchar(50) null, SkipFifoDotFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeletePlanTrackingLog (TABLE)
Columns: LoadingNo varchar(20), PlanOperation1 datetime2 null, ActOperation1 datetime2 null, ActOperation1By varchar(20) null, AlertFlagWarning1 int null, AlertFlagOverdue1 int null, Gate varchar(20) null, Note1 varchar(100) null, PlanOperation2 datetime2 null, ActOperation2 datetime2 null, ActOperation2By varchar(20) null, AlertFlagWarning2 int null, AlertFlagOverdue2 int null, Note2 varchar(100) null, PlanOperation3 datetime2 null, ActOperation3 datetime2 null, ActOperation3By varchar(20) null, AlertFlagWarning3 int null, AlertFlagOverdue3 int null, Note3 varchar(100) null, PlanOperation4 datetime2 null, ActOperation4 datetime2 null, ActOperation4By varchar(20) null, AlertFlagWarning4 int null, AlertFlagOverdue4 int null, Note4 varchar(100) null, PlanOperation5 datetime2 null, ActOperation5 datetime2 null, ActOperation5By varchar(20) null, AlertFlagWarning5 int null, AlertFlagOverdue5 int null, Note5 varchar(100) null, PlanOperation6 datetime2 null, ActOperation6 datetime2 null, ActOperation6By varchar(20) null, ApproveOperation6 datetime2 null, AlertFlagWarning6 int null, AlertFlagOverdue6 int null, Note6 varchar(100) null, LicensePlate varchar(20) null, Segment varchar(20) null, Transportation varchar(50) null, Shift varchar(10) null, FcsTruckType varchar(20) null, ActOperation7 datetime2 null, ActOperation7By varchar(20) null, Note7 varchar(100) null, ActOperation8 datetime2 null, ActOperation8By varchar(20) null, Note8 varchar(100) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeleteStickerMaster (TABLE)
PK: StickerNo
Columns: StickerNo bigint, StickerCode varchar(20), StickerBarcode varchar(50), MainStockLocation varchar(150) null, SpareStockLocation varchar(150) null, ReorderPoint int null, Warehouse varchar(20) null, StickerImg varchar(50) null, Status varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeleteStickerTransaction (TABLE)
Columns: StickerTransactionID bigint, Warehouse varchar(20) null, TransCode varchar(20) null, LoadingNo varchar(20) null, GTCode varchar(20) null, StickerCode varchar(20) null, StickerBarcode varchar(20) null, Qty int null, OldStickerQty int null, Remark nvarchar(100) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeleteTiresCheckerPlan (TABLE)
Columns: LoadingNo varchar(20), DONo varchar(20) null, GTCode varchar(20), Grade varchar(20) null, DoLine varchar(20) null, BSJCode varchar(20) null, LoadingQTY numeric(18,4) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.DeleteTiresCheckerScanLog (TABLE)
Columns: ScanLogID bigint, LoadingNo varchar(20), DONo varchar(20) null, Warehouse varchar(20) null, SerialNo varchar(20), GTCode varchar(20) null, SizeCode varchar(20) null, Weight numeric(15,6) null, ScanTireFlag int, ErrorCode varchar(50) null, ErrorDate datetime2 null, ScanDate datetime2, ScanBy varchar(20), UnlockDate datetime2 null, UnlockBy varchar(20) null, Gate varchar(20) null, StationName varchar(50) null, MachineName varchar(50) null, ProductionDate datetime2 null, ProductionWeek varchar(20) null, Grade varchar(20) null, ReceiveDetailId int null, SkipFifoDotFlag int null, DeleteDate datetime2 null

### dbo.DeleteTiresCheckerStickerSetting (TABLE)
PK: Id
Columns: Id bigint, LoadingNo varchar(20) null, GtCode varchar(20) null, SizeCode varchar(20) null, StickerBarcode varchar(50) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### dbo.Delivery (TABLE)
PK: LoadingNo, DONo
Columns: DONo varchar(20), LoadingNo varchar(20), InvoiceNo varchar(20) null, DONoFile varchar(20) null, SuffixNo varchar(20) null, CustomerCode varchar(20), CustomerName varchar(150) null, ShipTo varchar(255) null, Province varchar(20) null, BF varchar(20) null, PlanDeliveryDate datetime2 null, ChangeDestination int null, ChangeDestinationBy varchar(20) null, ChangeDestinationDate datetime2 null, OTP varchar(10) null, ReferenceNo varchar(10) null, ApprovedDate datetime2 null, ETA datetime2 null, PendingFlag int null, PendingDate datetime2 null, ResendFlag int null, ResendDate datetime2 null, DistributionCenterFlag int null, DistributorCode varchar(50) null, ArrivedAtDistributionCenterBy varchar(20) null, ArrivedAtDistributionCenterDate datetime2 null, StartFromDistributionCenterBy varchar(20) null, StartFromDistributionCenterDate datetime2 null, SendStatus varchar(20) null, SendDate datetime2 null, SendBy varchar(20) null, SendLatitude numeric(12,9) null, SendLongtitude numeric(12,9) null, ImportDistributorStatusId int null, ImportDOStatusId int null, DOReceiveStatus varchar(20) null, ReceiveDate datetime2 null, DelayReasonId int null, DelayReason varchar(150) null, ImportDelayReasonID int null, CompleteStatus varchar(20) null, DOReceiveBy varchar(20) null, ReceiveLatitude float null, ReceiveLongtitude float null, LatestProofType varchar(20) null, LatestAction varchar(20) null, GPSStatus int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, ETATruckDepart datetime2 null, ETADistributorDepart datetime2 null, DelayFlag int null, DelayTime int null, DistributorDelayTime int null, ETAFinal datetime2 null
FK: ImportDistributorStatusId -> import.ImportDistributorReceive.ImportID; ImportDelayReasonID -> import.ImportDelayReason.ImportID; DistributorCode -> dbo.Transportation.TransportationCode

### dbo.DeliveryAdditional (TABLE)
PK: DONo, QTNo
Columns: DONo varchar(20), QTNo varchar(20), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DeliveryDetail (TABLE)
PK: DONo, LoadingNo, SaleCode
Columns: DONo varchar(20), LoadingNo varchar(20), SaleCode varchar(20), GTCode varchar(20), SpecCode varchar(10), SizeName varchar(50), Grade varchar(20) null, DocnoNo varchar(20) null, DoLine varchar(20) null, BSJCode varchar(20) null, TranQty numeric(18,4), ProductionDate datetime2 null, LoadedQty numeric(18,4) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DeliveryEvidence (TABLE)
PK: DeliveryEvidenceId
Columns: DeliveryEvidenceId int, LoadingNo varchar(20), DONo varchar(20), DeliveryEvidenceTypeId int, Seq int, DeliveryEvidenceNote varchar(500), CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### dbo.DeliveryOrder (TABLE)
PK: DONo, TransferNo
Columns: DONo varchar(20), TransferNo varchar(20), CustomerCode varchar(20), CustomerName varchar(150) null, ShipTo varchar(255) null, ProvinceCode varchar(20) null, BF varchar(50) null, DeliveryDate datetime2 null, Market varchar(20) null, Warehouse varchar(20) null, DONoFile varchar(200) null, DOStatus varchar(20) null, Noted varchar(20) null, NotedReason varchar(20) null, Remark varchar(200) null, GenerateNote varchar(255) null, GenerateDate datetime2 null, SapImportRefId int null, SapImportMergeAt datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, ConvertPendingFlag int null, DeliveryMethod varchar(20) null, SapDeliveryDate datetime2 null

### dbo.DeliveryOrderCancellation (TABLE)
PK: CancelId
Columns: CancelId bigint, DONo varchar(20) null, TransferNo varchar(20) null, CustomerCode varchar(20) null, CustomerName varchar(150) null, ShipTo varchar(255) null, ProvinceCode varchar(20) null, BF varchar(50) null, DeliveryDate datetime2 null, Market varchar(20) null, Warehouse varchar(20) null, DONoFile varchar(200) null, DOStatus varchar(20) null, Noted varchar(20) null, NotedReason varchar(20) null, Remark varchar(200) null, GenerateNote varchar(255) null, GenerateDate datetime2 null, SapImportRefId int null, SapImportMergeAt datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, ConvertPendingFlag int null, DeliveryMethod varchar(20) null, SapDeliveryDate datetime2 null, CancelledDate datetime2 null, CancelledBy nvarchar(100) null, Reason nvarchar(500) null, EmailNotificationStatus varchar(50) null, EmailNotificationCompletedAt datetime2 null, ProcessDate datetime2 null, ProcessBy varchar(20) null
FK: CancelId -> dbo.DeliveryOrderCancellation.CancelId

### dbo.DeliveryOrderCancellationDetail (TABLE)
PK: Id
Columns: Id bigint, CancelId bigint null, DONo varchar(20) null, TransferNo varchar(20) null, GTCode varchar(20) null, SaleCode varchar(20) null, SpecCode varchar(10) null, SizeName varchar(50) null, TranQty numeric(18,4) null, FileCreatedDate datetime2 null, PartialFlag bit null, SapPlant varchar(50) null, SapStorageLocation varchar(50) null, SapWarehouse varchar(20) null, SapImportRefId int null, SapImportMergeAt datetime2 null, SapDeliveryItem varchar(200) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, ProcessDate datetime2 null, ProcessBy varchar(20) null
FK: CancelId -> dbo.DeliveryOrderCancellation.CancelId

### dbo.DeliveryOrderDetail (TABLE)
PK: DONo, TransferNo, GTCode
Columns: DONo varchar(20), TransferNo varchar(20), GTCode varchar(20), SaleCode varchar(20), SpecCode varchar(10), SizeName varchar(50), TranQty numeric(18,4), FileCreatedDate datetime2 null, PartialFlag bit null, SapPlant varchar(50) null, SapStorageLocation varchar(50) null, SapWarehouse varchar(20) null, SapImportRefId int null, SapImportMergeAt datetime2 null, SapDeliveryItem varchar(200) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DeliveryReceiveEvidence (TABLE)
PK: ReceiveId
Columns: ReceiveId int, LoadingNo varchar(20), DONo varchar(20), ReceivedStatus varchar(20), ReceivedDate datetime2, Latitude numeric(12,9) null, Longitude numeric(12,9) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Delivery.LoadingNo; DONo -> dbo.Delivery.DONo

### dbo.DeliveryReceiveOther (TABLE)
PK: Id
Columns: Id int, ReceiveId int, LoadingNo varchar(20), DONo varchar(20), ReceivedStatus varchar(20), ReceiverType varchar(20), Receiver varchar(100), Latitude numeric(12,9) null, Longitude numeric(12,9) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Delivery.LoadingNo; ReceiveId -> dbo.DeliveryReceiveEvidence.ReceiveId; DONo -> dbo.Delivery.DONo

### dbo.DeliveryReceivePasscode (TABLE)
PK: Id
Columns: Id int, ReceiveId int, LoadingNo varchar(20), DONo varchar(20), ReceivedStatus varchar(20), Latitude numeric(12,9) null, Longitude numeric(12,9) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Delivery.LoadingNo; ReceiveId -> dbo.DeliveryReceiveEvidence.ReceiveId; DONo -> dbo.Delivery.DONo

### dbo.DeliveryReceivePhoto (TABLE)
PK: PhotoId
Columns: PhotoId int, ReceiveId int, LoadingNo varchar(20), DONo varchar(20), ReceivedStatus varchar(20), PhotoType varchar(20), StoreFilePath varchar(500), Latitude numeric(12,9) null, Longitude numeric(12,9) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Delivery.LoadingNo; ReceiveId -> dbo.DeliveryReceiveEvidence.ReceiveId; DONo -> dbo.Delivery.DONo

### dbo.DeliveryReceiveSignature (TABLE)
PK: SignatureId
Columns: SignatureId int, ReceiveId int, LoadingNo varchar(20), DONo varchar(20), ReceivedStatus varchar(20), StoreFilePath varchar(100), Latitude numeric(12,9) null, Longitude numeric(12,9) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Delivery.LoadingNo; ReceiveId -> dbo.DeliveryReceiveEvidence.ReceiveId; DONo -> dbo.Delivery.DONo

### dbo.DeliveryRejectReason (TABLE)
PK: DONo, LoadingNo, ReasonCode
Columns: DONo varchar(20), LoadingNo varchar(20), ReasonCode varchar(20), Note nvarchar(150) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DeliverySerial (TABLE)
PK: DONo, LoadingNo, RecNo
Columns: DONo varchar(20), LoadingNo varchar(20), RecNo int, SerialNo varchar(20) null, SerialReceiveStatus varchar(20) null, ScanDate datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.DistributorReceive (TABLE)
PK: ReceiveId
Columns: ReceiveId int, LoadingNo varchar(20), DoNo varchar(20), ReceiveStatus varchar(20), ReceiveDate datetime2, DriverId int, Latitude numeric(12,9) null, Longitude numeric(12,9) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Delivery.LoadingNo; DriverId -> dbo.UserLogin.UserID; DoNo -> dbo.Delivery.DONo

### dbo.DistributorReceivePhoto (TABLE)
PK: PhotoId
Columns: PhotoId int, ReceiveId int, PhotoType varchar(20), StoreFilePath varchar(500), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: ReceiveId -> dbo.DistributorReceive.ReceiveId

### dbo.DistributorReceiveSignature (TABLE)
PK: SignatureId
Columns: SignatureId int, ReceiveId int, StoreFilePath varchar(100), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: ReceiveId -> dbo.DistributorReceive.ReceiveId

### dbo.DistributorRejectReason (TABLE)
PK: DONo, LoadingNo, ReasonCode
Columns: DONo varchar(20), LoadingNo varchar(20), ReasonCode varchar(20), Note nvarchar(150) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.EmailReceiverConfig (TABLE)
PK: ConfigId
Columns: ConfigId int, Warehouse varchar(20) null, OperationId varchar(20) null, EmailReceiver varchar(500) null, EmailCc varchar(500) null, MobileNo varchar(500) null, NotifyType varchar(20) null, NotifyMessage varchar(255) null, Description varchar(255) null

### dbo.FavoritesScreen (TABLE)
PK: UserID, ScreenCode
Columns: UserID int, ScreenCode varchar(20), SequenceNo int, CreateDate datetime2 null

### dbo.GiveAwayItem (TABLE)
PK: Warehouse, GtCode
Columns: Warehouse varchar(20), GtCode varchar(20), CreateDate datetime2 null, CreateBy varchar(20) null

### dbo.GTCodeMapping (TABLE)
PK: GTCode, SizeCode
Columns: GTCode varchar(20), SizeCode varchar(20)

### dbo.GuardHouse (TABLE)
PK: LoadingNo
Columns: LoadingNo varchar(20), Weight numeric(15,6) null, DriverID varchar(20) null, LicensePlate varchar(100) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.HoldTiresList (TABLE)
PK: SerialNo, Warehouse
Columns: SerialNo varchar(20), Warehouse varchar(20), AddDate datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null

### dbo.ImportPlanTimeSpanRep (TABLE)
PK: Vehicle
Columns: Vehicle varchar(20), PickingPeriod int null, LoadingPeriod int null

### dbo.LicensePlateMapping (TABLE)
PK: LicensePlateId
Columns: LicensePlateId int, LicensePlate varchar(100) null, TransportationCode varchar(20) null, TruckTypeCode varchar(20) null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.ListMaster (TABLE)
PK: ListCode
Columns: ListCode varchar(20), MasterCode varchar(20) null, Description varchar(255) null

### dbo.ListMasterDetail (TABLE)
PK: ListCode, MasterCode
Columns: ListCode varchar(20), MasterCode varchar(20), MasterName varchar(100) null, Value1 varchar(255) null, Value2 varchar(255) null, Value3 varchar(255) null, Value4 varchar(255) null, Value5 varchar(25) null, Description varchar(255) null, SeqNo int null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null

### dbo.LoadingNoMapping (TABLE)
Columns: Prefix varchar(20) null, MasterWarehouse varchar(30) null

### dbo.LocationTracking (TABLE)
PK: Id
Columns: Id int, VisitorId nvarchar(250) null, Latitude float, Longitude float, CreatedAt datetime

### dbo.LtDeliveryBsToCustomer (TABLE)
PK: FcsWarehouseCode, CustomerCode, DeliveryMethod
Columns: FcsWarehouseCode varchar(20), CustomerCode varchar(20), DeliveryMethod varchar(20), LeadTimeHour int null, CreatedDate datetime2, CreatedBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: CustomerCode -> dbo.Customer.CustomerCode

### dbo.LtDeliveryBsToDistributor (TABLE)
PK: FcsWarehouseCode, DeliveryMethod
Columns: FcsWarehouseCode varchar(20), DeliveryMethod varchar(20), LeadTimeHour int null, CreatedDate datetime2, CreatedBy varchar(50), UpdateDate datetime2, UpdateBy varchar(50)

### dbo.LtDeliveryDistributorToCustomer (TABLE)
PK: CustomerCode, DeliveryMethod
Columns: CustomerCode varchar(20), DeliveryMethod varchar(20), LeadTimeHour int null, CreatedDate datetime, CreatedBy varchar(20), UpdateDate datetime null, UpdateBy varchar(20) null

### dbo.MenuGroup (TABLE)
PK: MenuGroupCode
Columns: MenuGroupCode varchar(20), MenuGroupName varchar(50) null, IconName varchar(50) null, ThumbnailFileName varchar(50) null, BackgroundColor varchar(50) null, SeqNo int null

### dbo.OperationLog (TABLE)
PK: LoadingNo, LogTime
Columns: LoadingNo varchar(20), LogTime datetime2, OperationTime datetime2 null, Operation varchar(20) null, ScreenCode varchar(20) null, ActualFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null

### dbo.PartTagApproval (TABLE)
PK: PartTagID
Columns: PartTagID bigint, LoadingNo varchar(20) null, PartTag varchar(20) null, QTY int null

### dbo.Plan (TABLE)
PK: LoadingNo
Columns: LoadingNo varchar(20), ShippingNo varchar(20) null, Market varchar(20) null, Warehouse varchar(20) null, MasterWarehouse varchar(20) null, WhNo varchar(20) null, Plant varchar(20) null, Sloc varchar(20) null, SubmitPlanTiresChecker int null, LoadingStatus varchar(20) null, PlanCreateDate datetime2 null, LoadFinishDate datetime2 null, FileName varchar(20) null, PlanDate datetime2 null, TruckNote varchar(50) null, TruckSize varchar(100) null, LoadRemark varchar(100) null, ExportShippingAgentFlag int null, ImportId int null, SkipFifoDotFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.PlanTracking (TABLE)
PK: LoadingNo
Columns: LoadingNo varchar(20), PlanOperation1 datetime2 null, ActOperation1 datetime2 null, ActOperation1By varchar(20) null, AlertFlagWarning1 int null, AlertFlagOverdue1 int null, Gate varchar(20) null, Note1 varchar(100) null, PlanOperation2 datetime2 null, ActOperation2 datetime2 null, ActOperation2By varchar(20) null, AlertFlagWarning2 int null, AlertFlagOverdue2 int null, Note2 varchar(100) null, PlanOperation3 datetime2 null, ActOperation3 datetime2 null, ActOperation3By varchar(20) null, AlertFlagWarning3 int null, AlertFlagOverdue3 int null, Note3 varchar(100) null, PlanOperation4 datetime2 null, ActOperation4 datetime2 null, ActOperation4By varchar(20) null, AlertFlagWarning4 int null, AlertFlagOverdue4 int null, Note4 varchar(100) null, PlanOperation5 datetime2 null, ActOperation5 datetime2 null, ActOperation5By varchar(20) null, AlertFlagWarning5 int null, AlertFlagOverdue5 int null, Note5 varchar(100) null, PlanOperation6 datetime2 null, ActOperation6 datetime2 null, ActOperation6By varchar(20) null, ApproveOperation6 datetime2 null, AlertFlagWarning6 int null, AlertFlagOverdue6 int null, Note6 varchar(100) null, LicensePlate varchar(100) null, Segment varchar(20) null, Transportation varchar(50) null, Shift varchar(10) null, FcsTruckType varchar(20) null, ActOperation7 datetime2 null, ActOperation7By varchar(20) null, Note7 varchar(100) null, ActOperation8 datetime2 null, ActOperation8By varchar(20) null, Note8 varchar(100) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.Province (TABLE)
PK: Code
Columns: Code varchar(10), NameTh varchar(100), NameEn varchar(100), ProvinceLatitude float null, ProvinceLongtitude float null, DistanceFromRS numeric(18,4) null, DistanceFromNK numeric(18,4) null, DistanceFromNK24 numeric(18,4) null, DistanceFromCH numeric(18,4) null, DistanceFromAM numeric(18,4) null, Area varchar(20) null

### dbo.RejectReason (VIEW)
Columns: ReasonCode varchar(20), Name varchar(100) null, Description varchar(255) null, IsSpecify varchar(255) null, SeqNo int null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null

### dbo.RoutingActivityLog (TABLE)
PK: LogID
Columns: LogID int, Username varchar(20), ActivityDate datetime2, ScreenCode varchar(20), EventLog varchar(50), SubEvent varchar(50) null, RoutingNo varchar(20) null, DONo varchar(20) null, TransferNo varchar(20) null, Note varchar(255) null, IPAdress varchar(max) null, Warehouse varchar(20) null, ApplicationName varchar(50) null

### dbo.RoutingDriven (TABLE)
PK: RoutingNo
Columns: RoutingNo bigint, DriverID int null, Latitude float null, Longtitude float null, DeliveryFlag int null

### dbo.RoutingReturn (TABLE)
PK: RoutingID, DONo
Columns: RoutingID bigint, DONo varchar(20), LoadingNo varchar(20), UserID int, ReturnDate datetime2 null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.RoutingTracking (TABLE)
PK: UserID
Columns: UserID int, Latitude float, Longitude float, ActiveFlag int, ExpireFlag int, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### dbo.RunningNo (TABLE)
Columns: RunningCode nvarchar(30), Description nvarchar(50) null, RunningFormat nvarchar(50) null, RunningDigit int, ResetFlag nvarchar(1), Remark nvarchar(255) null, TableName nvarchar(50) null, ColumnName nvarchar(50) null

### dbo.RunningNoItem (TABLE)
Columns: RunningCode nvarchar(20), Period int, CurrentNo int

### dbo.Screen (TABLE)
PK: ScreenCode
Columns: ScreenCode varchar(20), ScreenName varchar(50) null, Controller varchar(50) null, Action varchar(50) null, Param1 varchar(50) null, Param2 varchar(50) null, Param3 varchar(50) null, MenuGroupCode varchar(20) null, ParentScreenCode varchar(20) null, ParentFunctionCode varchar(20) null, SeqNo int null, ScreenType varchar(20) null, ObjectType varchar(20) null, MenuFlag int null

### dbo.StickerMaster (TABLE)
PK: StickerCode, Warehouse
Columns: StickerCode varchar(20), Warehouse varchar(20), StickerBarcode varchar(50), MainStockLocation varchar(150) null, SpareStockLocation varchar(150) null, ReorderPoint int null, MaximumPoint int null, StickerImg varchar(255) null, BarcodeScanFlag int null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.StickerMovementAverage (TABLE)
PK: StickerCode, Warehouse
Columns: StickerCode varchar(20), Warehouse varchar(20), MovementAverage int null

### dbo.StickerStock (TABLE)
PK: StickerCode, Warehouse
Columns: StickerCode varchar(20), Warehouse varchar(20), StickerBarcode varchar(50), Qty int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.StickerTransaction (TABLE)
PK: StickerTransactionID
Columns: StickerTransactionID bigint, TransactionDate datetime2 null, Warehouse varchar(20) null, TransCode varchar(20) null, LoadingNo varchar(20) null, GTCode varchar(20) null, StickerCode varchar(20) null, StickerBarcode varchar(50) null, Qty int null, OldStickerQty int null, Remark nvarchar(100) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.StockTaking (TABLE)
PK: StockTakingId
Columns: StockTakingId int, Warehouse varchar(20), Market varchar(20), KeepDate datetime2, KeepBy varchar(20) null, UploadFlag int null, UploadDate datetime2 null, UploadBy varchar(20) null, AdjustFlag int null, AdjustDate datetime2 null, AdjustBy varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.StockTakingDetail (TABLE)
PK: StockTakingDetailId
Columns: StockTakingDetailId int, StockTakingId int, StickerCode varchar(20) null, StickerBarcode varchar(50) null, SystemQty int null, CountQty int null, DiffQty int null, AdjustFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.StockTakingLog (TABLE)
PK: LogId
Columns: LogId int, StockTakingId int null, Warehouse varchar(20) null, Market varchar(20) null, KeepDate datetime2 null, KeepBy varchar(20) null, FileName varchar(200) null, CreateDate datetime2 null, CreateBy varchar(20) null

### dbo.sysdiagrams (TABLE)
PK: diagram_id
Columns: name nvarchar(128), principal_id int, diagram_id int, version int null, definition varbinary(max) null

### dbo.TireCheckerFifoDot (TABLE)
PK: Warehouse, CustomerCode, GtCode
Columns: Warehouse varchar(20), CustomerCode varchar(20), GtCode varchar(20), ProductionDate datetime2 null, ProductionWeek varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TireCheckerFifoDotHist (TABLE)
PK: HistId
Columns: HistId int, Warehouse varchar(20) null, CustomerCode varchar(20) null, GtCode varchar(20) null, ProductionDate datetime2 null, ProductionWeek varchar(20) null, LoadingNo varchar(20) null, CreateDate datetime null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TiresChecker (TABLE)
PK: LoadingNo
Columns: LoadingNo varchar(20), Market varchar(20) null, ContainerNo varchar(20) null, InvoiceNo varchar(20) null, ShipDate datetime2 null, SkipPrepareStickerFlag int null, StickerPrepareFlag int null, Remark nvarchar(100) null, WeightStartDate datetime2 null, WeightFinishDate datetime2 null, CheckerStartDate datetime2 null, CheckerFinishDate datetime2 null, CheckerGate varchar(20) null, CheckerWarehouse varchar(20) null, StartScanLoadingFlag int null, StartScanLoadingDate datetime2 null, StartScanLoadingBy varchar(20) null, SkipUploadWmsFlag int null, ConfirmLoadingCompleteFlag int null, ConfirmLoadingCompleteDate datetime2 null, ConfirmLoadingCompleteBy varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TiresCheckerEndLoadLog (TABLE)
PK: EndLoadId
Columns: EndLoadId int, Warehouse varchar(20) null, LoadingNo varchar(20) null, ScanBarcodeFlag int null, ReasonCode varchar(20) null, EndLoadDate datetime2 null, EndLoadBy varchar(20) null, ApproveDate datetime2 null, ApproveBy varchar(20) null, Gate varchar(20) null, StationName varchar(50) null, MachineName varchar(50) null

### dbo.TiresCheckerPlan (TABLE)
PK: TCPlanID
Columns: TCPlanID bigint, LoadingNo varchar(20), DONo varchar(20) null, GTCode varchar(20), Grade varchar(20) null, DoLine varchar(20) null, BSJCode varchar(20) null, LoadingQTY numeric(18,4) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TiresCheckerScanLog (TABLE)
PK: ScanLogID
Columns: ScanLogID bigint, LoadingNo varchar(20), DONo varchar(20) null, Warehouse varchar(20) null, SerialNo varchar(20), GTCode varchar(20) null, SizeCode varchar(20) null, Weight numeric(15,6) null, ScanTireFlag int, ErrorCode varchar(50) null, ErrorDate datetime2 null, ScanDate datetime2, ScanBy varchar(20), UnlockDate datetime2 null, UnlockBy varchar(20) null, Gate varchar(20) null, StationName varchar(50) null, MachineName varchar(50) null, ProductionDate datetime2 null, ProductionWeek varchar(20) null, Grade varchar(20) null, ReceiveDetailId int null, SkipFifoDotFlag int null
FK: ReceiveDetailId -> rc.ReceiveItemDetail.ReceiveDetailId

### dbo.TiresCheckerScanStickerLog (TABLE)
PK: ScanStickerLogID
Columns: ScanStickerLogID bigint, LoadingNo varchar(20), DONo varchar(20) null, SerialNo varchar(20), GTCode varchar(20), SizeCode varchar(20), StickerBarcode varchar(50), ScanDate datetime2

### dbo.TiresCheckerSticker (TABLE)
PK: TC_StickerID
Columns: TC_StickerID bigint, LoadingNo varchar(20), GTCode varchar(20), StickerCode varchar(20), IsUsed int null, StickerPlanQTY numeric(18,4) null, StickerPrepareQTY numeric(18,4) null, StickerPrepareOldQty numeric(18,4) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TiresCheckerStickerSetting (TABLE)
PK: Id
Columns: Id bigint, LoadingNo varchar(20) null, GtCode varchar(20) null, SizeCode varchar(20) null, StickerBarcode varchar(50) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TiresMaster (TABLE)
PK: TiresCode
Columns: TiresCode varchar(20), Warehouse varchar(20), GTCode varchar(20), SaleCode varchar(20) null, SizeCode varchar(20) null, SizeName varchar(50) null, TiresSize varchar(20) null, StructureCode varchar(20) null, BPCPlus varchar(20) null, GTType varchar(20) null, JigType varchar(20) null, JigCode varchar(20) null, PartTag varchar(20) null, WeightStd numeric(15,6) null, Capacity numeric(15,6) null, TireBrand varchar(20) null, DomesticType varchar(20) null, BookQuantity int null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TiresSticker (TABLE)
PK: TiresMasterID
Columns: TiresMasterID bigint, TiresCode varchar(20), StickerCode varchar(20) null, StickerMarket varchar(20) null, StickerCust varchar(20) null, CustomerGroupCode varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.Transportation (TABLE)
PK: TransportationCode
Columns: TransportationCode varchar(50), TransportationName varchar(200), TransportationFullName varchar(200) null, FcsTransporterCode varchar(20) null, GroupCode varchar(20) null, PercentDiscount decimal(5,2) null, DistributorFlag int null, Destination int null, DefaultDriverId int null, FreightDisplayFlag int null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.TransportationDashboard (TABLE)
PK: TransportationCode
Columns: TransportationCode varchar(50), DashboardCode varchar(50) null, ColorCode varchar(20) null, ColorCodeAlternate varchar(20) null

### dbo.TransportationPlan (TABLE)
PK: PlatePlanID
Columns: PlatePlanID int, LoadingDate date, TransportationPlateID int, TransportationCode varchar(50), SeqNo int, ActiveFlag int, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: TransportationCode -> dbo.Transportation.TransportationCode

### dbo.TransportationPlanMapping (TABLE)
PK: MappingId
Columns: MappingId int, TruckNote varchar(100) null, TransportationCode varchar(50) null

### dbo.TransportationPlate (TABLE)
PK: TransportationPlateID
Columns: TransportationPlateID int, TransportationCode varchar(50), TruckTypeCode varchar(50), PlateNo varchar(10), PlateProvinceCode varchar(10), Capacity decimal(15,6) null, NetWeight decimal(15,6) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: TransportationCode -> dbo.Transportation.TransportationCode; PlateProvinceCode -> dbo.Province.Code

### dbo.TransportationRoute (TABLE)
PK: ZoneID
Columns: ZoneID int, TransportationCode varchar(50), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: TransportationCode -> dbo.Transportation.TransportationCode; ZoneID -> dbo.Zone.ZoneID

### dbo.TransportationTruckNoteMapping (TABLE)
Columns: TruckNote varchar(100) null

### dbo.TreadMarkImage (TABLE)
PK: Id
Columns: Id int, TiresCode varchar(20), ImagePath varchar(255), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: TiresCode -> dbo.TiresMaster.TiresCode

### dbo.UserGroup (TABLE)
PK: UserGroupID
Columns: UserGroupID varchar(50), GroupName varchar(50) null, FullMenuFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.UserGroupPermission (TABLE)
PK: UserGroupID, ScreenCode
Columns: UserGroupID varchar(50), ScreenCode varchar(50), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.UserLogin (TABLE)
PK: UserID
Columns: UserID int, Username varchar(20) null, Password varchar(255) null, BarcodeToken varchar(20) null, Name varchar(50) null, Surname varchar(50) null, UserType varchar(20) null, Transportation varchar(20) null, SetNextDropFlag int null, WinAppUnlock int null, UserStatus int null, UserGroup varchar(50) null, WarehouseCode varchar(20) null, Email varchar(50) null, Mobile varchar(20) null, LoginTime datetime2 null, ChangePasswordDate datetime2 null, MobileModel varchar(200) null, MobileSdkVersion varchar(200) null, MobileAppVersion varchar(200) null, LastVisitorId varchar(250) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null

### dbo.vvChannelAll (VIEW)
Columns: RowNo bigint null, LoadingNo varchar(30) null, DONo varchar(30) null, ChannelID int null, ChannelTypeID int, CustomerCode varchar(30) null, CustomerName varchar(150) null, OwnerId int, LastedOpenChannelAt datetime2 null, LastedSentMessageAt datetime2 null, IsActive bit null

### dbo.vvChannelNormal (VIEW)
Columns: LoadingNo varchar(20) null, DONo varchar(20) null, ChannelID int null, ChannelTypeID int, CustomerCode varchar(20) null, CustomerName varchar(150) null, OwnerId int, LastedOpenChannelAt datetime2 null, LastedSentMessageAt datetime2 null, IsActive bit null

### dbo.vvChannelOther (VIEW)
Columns: LoadingNo varchar(30) null, DONo varchar(30) null, ChannelID int null, ChannelTypeID int, CustomerCode varchar(30) null, CustomerName varchar(30) null, OwnerId int, LastedOpenChannelAt datetime2 null, LastedSentMessageAt datetime2 null, IsActive bit null

### dbo.vwActiveDriver (VIEW)
Columns: Transportation varchar(20) null, UserID int, Username varchar(20) null, Firstname varchar(50) null, Surname varchar(50) null, WarehouseCode varchar(20) null

### dbo.vwActiveTiresMaster (VIEW)
Columns: RowNo bigint null, TiresCode varchar(20), Warehouse varchar(20), GTCode varchar(20), SaleCode varchar(20) null, SizeCode varchar(20) null, SizeName varchar(50) null, TiresSize varchar(20) null, StructureCode varchar(20) null, BPCPlus varchar(20) null, GTType varchar(20) null, JigType varchar(20) null, JigCode varchar(20) null, PartTag varchar(20) null, WeightStd numeric(15,6) null, Capacity numeric(15,6) null, TireBrand varchar(20) null, DomesticType varchar(20) null, BookQuantity int null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.vwDelivery (VIEW)
Columns: DONo varchar(20), LoadingNo varchar(20), DONoFile varchar(20) null, SuffixNo varchar(20) null, CustomerCode varchar(20), CustomerName varchar(150) null, ShipTo varchar(255) null, Province varchar(20) null, BF varchar(20) null, PlanDeliveryDate datetime2 null, OTP varchar(10) null, ReferenceNo varchar(10) null, ApprovedDate datetime2 null, ETA datetime2 null, DOReceiveStatus varchar(20) null, ReceiveDate datetime2 null, DelayReason varchar(150) null, CompleteStatus varchar(20) null, DOReceiveBy varchar(20) null, ReceiveLatitude float null, ReceiveLongtitude float null, GPSStatus int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, CustomerCodeNoPrefix varchar(20) null

### dbo.vwDeliveryMarket (VIEW)
Columns: LoadingNo varchar(20), DONo varchar(20), MarketCode varchar(20) null, MarketName varchar(100) null

### dbo.vwDeliveryOrder (VIEW)
Columns: DONo varchar(20), TransferNo varchar(20), CustomerCode varchar(20), CustomerName varchar(150) null, ShipTo varchar(255) null, ProvinceCode varchar(20) null, BF varchar(50) null, DeliveryDate datetime2 null, Market varchar(20) null, Warehouse varchar(20) null, DONoFile varchar(200) null, DOStatus varchar(20) null, Noted varchar(20) null, NotedReason varchar(20) null, Remark varchar(200) null, GenerateNote varchar(255) null, GenerateDate datetime2 null, SapImportRefId int null, SapImportMergeAt datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, ConvertPendingFlag int null, DeliveryMethod varchar(20) null, CustomerCodeNoPrefix varchar(20) null, PlanDONo varchar(41)

### dbo.vwDeliveryOrderCapacity (VIEW)
Columns: DONo varchar(20), TransferNo varchar(20), WarehouseCode varchar(20) null, DOCapacity numeric(38,10) null

### dbo.vwImportDocsReceive (VIEW)
Columns: RowNo bigint null, DocsReceiveId int, ImportId int, DocsReceiveTypeId int, DocsReceiveTypeName varchar(20), DoNo varchar(20) null, TransportationCode varchar(50) null, TransportationName varchar(200) null, PeriodDate date null, MarketCode varchar(20) null, MarketName varchar(100) null, DocumentPeriodDate date null, IsScanned bit null, IsSigned bit null, FileName varchar(500), PageNo int, IsDuplicateDoNo bit null

### dbo.vwImportDocsReceiveDuplicate (VIEW)
Columns: DoNo varchar(20) null

### dbo.vwImportDoNo (VIEW)
Columns: DoNo varchar(20) null

### dbo.vwJobDescription (VIEW)
Columns: RoutingJobID bigint, LoadingNo varchar(20), DONo varchar(20), CreatePlanAs datetime2 null, CustomerCode varchar(20) null, CustomerName varchar(150) null, Address varchar(255) null, Mobile varchar(150) null, Latitude float null, Longitude float null, CompleteFlag int, ScanDate datetime2, CreateJobAs datetime2 null, RoutingID bigint null, UserID int null, NextDropFlag int null, DOReceiveStatus varchar(20) null, TotalJob int null, TotalCompleted int null, PendingFlag int null, PendingDate datetime2 null, ResendDate datetime2 null, ResendFlag int null, LatestAction varchar(20) null

### dbo.vwMasterDOStatus (VIEW)
Columns: StatusCode varchar(20), StatusName varchar(100) null

### dbo.vwMasterMarket (VIEW)
Columns: MarketCode varchar(20), MarketName varchar(100) null

### dbo.vwMasterNoted (VIEW)
Columns: NotedCode varchar(20), NotedName varchar(100) null

### dbo.vwMasterNotedReason (VIEW)
Columns: NotedReasonCode varchar(20), NotedReasonName varchar(100) null

### dbo.vwMasterRoutingStatus (VIEW)
Columns: StatusCode varchar(20), StatusName varchar(100) null

### dbo.vwMasterRoutingType (VIEW)
Columns: RoutingTypeCode varchar(20), RoutingTypeName varchar(100) null

### dbo.vwMasterWarehouse (VIEW)
Columns: WarehouseCode varchar(20), WarehouseFullName varchar(100) null, WarehouseShotName varchar(255) null

### dbo.vwRouteDO (VIEW)
Columns: No bigint null, SeqNo int, RoutingNo varchar(20), DONo varchar(20), TransferNo varchar(20), CustomerCode varchar(20), CustomerName varchar(150), ShipTo varchar(255), WarehouseCode varchar(20), WarehouseName varchar(100) null, MarketCode varchar(20) null, MarketName varchar(100) null, ZoneID int null, ZoneName varchar(100) null, NotedCode varchar(20), NotedName varchar(100)

### dbo.vwRouteTruck (VIEW)
Columns: No bigint null, RoutingNo varchar(20), TransportationCode varchar(50), TransportationPlateID int null, PlateNo varchar(10) null, PlateProvinceCode varchar(10) null, ZoneID int null, TruckTypeCode varchar(50) null, LoadingDate datetime2, WarehouseCode varchar(20) null, MarketCode varchar(20) null, TruckCapacity decimal(15,6)

### dbo.vwTireCheckerInquiryScreen (VIEW)
Columns: ScreenCode varchar(20), ScreenName varchar(50) null, Controller varchar(50) null, Action varchar(50) null, Param1 varchar(50) null, Param2 varchar(50) null, Param3 varchar(50) null, MenuGroupCode varchar(20) null, ParentScreenCode varchar(20) null, SeqNo int null, ScreenType varchar(20) null

### dbo.vwTireCheckerPlanQuantity (VIEW)
Columns: Warehouse varchar(20) null, StickerCode varchar(20), EstimatePlanQty numeric(38,4) null

### dbo.vwTireMasterGroupByGTCode (VIEW)
Columns: GTCode varchar(20), SizeName varchar(50) null, SizeCode varchar(20) null, WeightStd numeric(15,6) null, WeightMin numeric(30,12) null, WeightMax numeric(30,12) null, WeightPercentage decimal(9,2) null, JigType varchar(20) null, JigCode varchar(20) null, GTType varchar(20) null, Warehouse varchar(20), RowNo bigint null

### dbo.vwTiresCheckerScanned (VIEW)
Columns: Warehouse varchar(20) null, LoadingNo varchar(20), DONo varchar(20) null, GTCode varchar(20) null, ProductionDate datetime2 null, ProductionWeek varchar(20) null

### dbo.vwTiresMasterDetail (VIEW)
Columns: TiresMasterID bigint, TiresCode varchar(20), StickerCode varchar(20) null, StickerMarket varchar(20) null, StickerCust varchar(20) null, CustomerName varchar(150) null, CustomerGroupCode varchar(20) null, CustomerGroupName varchar(100) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, StickerBarcode varchar(50) null, StickerImg varchar(255) null

### dbo.vwTransportationPlan (VIEW)
Columns: No bigint null, SeqNo int, PlateNo varchar(10), PlateProvinceName varchar(100) null, TruckCapacity decimal(15,6) null, TransportationCode varchar(50), TruckTypeCode varchar(50), LoadingDate date, PlateProvinceCode varchar(10), TransportationPlateID int, ZoneID int null, ZoneName varchar(100) null, ActiveFlag int

### dbo.vwUnknownTransporterRequest (VIEW)
Columns: RowNo bigint null, DoNo varchar(20) null, TransportationCode varchar(50), TransportationName varchar(200), PeriodDate datetime2

### dbo.vwZone (VIEW)
Columns: ZoneID int null, ZoneCode varchar(100) null, ZoneName varchar(100) null, ZoneLevel int null, ZoneTree varchar(200) null, ParentZoneID int null, ZoneSort varchar(200) null, RootZoneCode nvarchar(20) null

### dbo.WarehouseTask (TABLE)
PK: WarehouseTaskId
Columns: WarehouseTaskId int, DONo varchar(20) null, BPCPlus varchar(20) null, GTCode varchar(20) null, OriginalGTCode varchar(20) null, WarehouseOrder varchar(20) null, BatchNo varchar(20) null, Qty int null, StorageBin varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.WeightScale (TABLE)
PK: LoadingNo, GTCode
Columns: LoadingNo varchar(20), GTCode varchar(20), SizeCode varchar(20), WeightQTY int null, TotalWeight numeric(15,6) null, AvgWeight numeric(15,6) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.WeightScaleLog (TABLE)
PK: WeightScaleLogID
Columns: WeightScaleLogID bigint, LoadingNo varchar(20), GTCode varchar(20), SizeCode varchar(20), Warehouse varchar(20), Weight numeric(15,6), WeightMin numeric(15,6), WeightMax numeric(15,6), QTY int, RemovedFlag int null, RemovedBy varchar(20) null, RemovedDate datetime2 null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.z_DuplicateFreightRate (TABLE)
Columns: FreightRateMasterUnitId float null, Market nvarchar(255) null, Transportation nvarchar(255) null, Departure float null, Departurename nvarchar(255) null, Destination float null, DestinationName nvarchar(255) null, Distance float null, Selected nvarchar(255) null, F10 nvarchar(255) null

### dbo.z_MigrateBrand (TABLE)
Columns: TiresCode varchar(20) null, GTCode varchar(20) null, Warehouse varchar(20) null, Brand varchar(20) null

### dbo.z_TruckTypeMapping (TABLE)
PK: VehicleId
Columns: VehicleId int, VehicleCode varchar(20) null, VehicleName varchar(50) null, TruckTypeCode varchar(20) null

### dbo.Zone (TABLE)
PK: ZoneID
Columns: ZoneID int, ParentZoneID int, ZoneCode varchar(100), ZoneName varchar(100), ZoneLevel int, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### dbo.zz_20211115_vwPlanTransporter (VIEW)
Columns: LoadingNo varchar(20), ShippingNo varchar(20) null, Market varchar(20) null, Warehouse varchar(20) null, MasterWarehouse varchar(20) null, WhNo varchar(20) null, Plant varchar(20) null, Sloc varchar(20) null, SubmitPlanTiresChecker int null, LoadingStatus varchar(20) null, PlanCreateDate datetime2 null, LoadFinishDate datetime2 null, FileName varchar(20) null, PlanDate datetime2 null, TruckNote varchar(50) null, TruckSize varchar(100) null, LoadRemark varchar(100) null, ExportShippingAgentFlag int null, ImportId int null, SkipFifoDotFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, TransporterCode varchar(28)

### bins.PrintLog (TABLE)
PK: LogId
Columns: LogId int, WarehouseCode varchar(20) null, BarcodeNo varchar(20) null, GtCode varchar(20) null, SizeCode varchar(20) null, BookingQty int null, PrintDate datetime2 null

### bins.ScanLog (TABLE)
PK: LogId
Columns: LogId int, ScanDate datetime2 null, WarehouseCode varchar(20) null, TagP9 varchar(20) null, GtCode varchar(20) null, Qty int null

### bins.SerialRunningNo (TABLE)
Columns: WarehouseCode varchar(20) null, SizeCode varchar(20) null, RunningNo varchar(20) null, UpdateDate datetime2 null

### bins.TireScrap (TABLE)
PK: ScrapId
Columns: ScrapId int, WarehouseCode varchar(20), ReportDate datetime2, ReportBy varchar(20), GtCode varchar(20), Qty int, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### bins.TireScrapDetail (TABLE)
PK: ScrapDetailId
Columns: ScrapDetailId int, ScrapId int, SerialNo varchar(20), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### bins.UnplanOut (TABLE)
PK: DocumentNo
Columns: DocumentNo varchar(20), WarehouseCode varchar(20), UploadFilename varchar(100) null, StoreFilename varchar(100) null, ReportDate datetime2 null, ReportBy varchar(20) null, ConfirmFlag int null, ConfirmDate datetime2 null, ConfirmBy varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### bins.UnplanOutBarcode (TABLE)
PK: Id
Columns: Id int, DocumentNo varchar(20), SerialNo varchar(20), ConfirmFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### bins.UnplanOutDetail (TABLE)
PK: DetailId
Columns: DetailId int, DocumentNo varchar(20), GtCode varchar(20), Qty int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### captc.vwTWH_TireChecker (VIEW)
Columns: No bigint null, Warehouse varchar(100) null, SerialNo varchar(20), GTCode varchar(20), BPCPlus varchar(20) null, SizeName varchar(50) null, LoadingNo varchar(20), PickingDate datetime2 null, SendingDate datetime2 null, CustomerName varchar(150) null, ShipTo varchar(255) null, QTY int null

### captc.vwTWH_TireCheckerReceiving (VIEW)
Columns: No bigint null, Warehouse varchar(100) null, SerialNo varchar(20), GtCode varchar(20), QTY int null, ReceivedDate datetime2, EwmTag varchar(100) null, Shift varchar(1) null

### chat.Attachments (TABLE)
PK: Id
Columns: Id int, MessageLogId int, StoreFileName varchar(500), DisplayFileName varchar(500), ActiveFlag int
FK: MessageLogId -> chat.MessageLog.Id

### chat.Channel (TABLE)
PK: Id
Columns: Id int, ChannelTypeId int, SubscriberName varchar(200), DONo varchar(20) null, LoadingNo varchar(20) null, ActiveFlag int, LastedAccess datetime2, CreateDate datetime2, CreateBy int
FK: CreateBy -> dbo.UserLogin.UserID; ChannelTypeId -> chat.ChannelType.Id

### chat.ChannelType (TABLE)
PK: Id
Columns: Id int, Name varchar(20)

### chat.MessageLog (TABLE)
PK: Id
Columns: Id int, ChannelID int, UserID int, Text varchar(max) null, ActiveFlag int, CreateDate datetime2
FK: UserID -> dbo.UserLogin.UserID; ChannelID -> chat.Channel.Id

### dot.DotChecking (TABLE)
PK: LoadingNo
Columns: LoadingNo varchar(20), WarehouseCode varchar(20), CustomerCode varchar(20), CustomerName varchar(150), PlanDate datetime, TiresAgeAcceptance int, AgingWeek varchar(20), ScanQty int, TotalQty int, EndLoadCode varchar(20), CreateDate datetime, CreateBy varchar(20), UpdateDate datetime null, UpdateBy varchar(20) null

### dot.DotCheckingLog (TABLE)
PK: DotCheckingId
Columns: DotCheckingId int, LoadingNo varchar(20), SerialNo varchar(20), GtCode varchar(20), SizeCode varchar(20), ProductionWeek varchar(20), ScanDate datetime, ScanBy varchar(20), CreateDate datetime, CreateBy varchar(20), UpdateDate datetime null, UpdateBy varchar(20) null
FK: LoadingNo -> dot.DotChecking.LoadingNo

### dotchk.DotCheckingInfo (TABLE)
PK: LoadingNo
Columns: LoadingNo varchar(20), ContainerNo varchar(20) null, CustomerCode varchar(20) null, CustomerName varchar(150) null, CheckDate datetime2 null, CheckBy varchar(20) null, InspectDate datetime2 null, InspectBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null

### dotchk.DotCheckingLogs (TABLE)
PK: ScanLogId
Columns: ScanLogId bigint, LoadingNo varchar(20), DoNo varchar(20) null, SerialNo varchar(20) null, GtCode varchar(20) null, SizeCode varchar(20) null, ProductionWeek varchar(20) null, ScanDate datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### fcs.BudgetGroupMaster (TABLE)
PK: BudgetGroupMasterId
Columns: BudgetGroupMasterId int, Departure int, Destination int, AccountCategory varchar(20), AccountCode varchar(50), BudgetGroupCode varchar(20), CreateBy varchar(20), CreateDate datetime2

### fcs.CalculationMapping (TABLE)
PK: CalculationMappingId
Columns: CalculationMappingId bigint, LoadingNo varchar(20), CalculationType varchar(20), ActionDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.CalculationTypeAdjustment (TABLE)
PK: CalculationTypeAdjustmentId
Columns: CalculationTypeAdjustmentId int, Warehouse varchar(20), Market varchar(20), Transportation varchar(50), TruckType varchar(20), Departure int, Destination int, NewDestination int, NewCalculationType varchar(20)

### fcs.ClaimSizeMapping (TABLE)
PK: SizeId
Columns: SizeId int, GroupCode nvarchar(50) null, GtType varchar(20) null, TireCategory varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### fcs.CnReturn (TABLE)
PK: ReturnId
Columns: ReturnId int, DoNo varchar(20), LoadingNo varchar(20), CustomerCode varchar(20), CustomerDestination int, DistributorCode varchar(50), DistributorDestination int, DeliveryDate datetime2, ReturnRate varchar(20), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### fcs.CnReturnDetail (TABLE)
PK: DetailId
Columns: DetailId int, ReturnId int, GtCode varchar(20), SaleCode varchar(20) null, SpecCode varchar(10) null, SizeName varchar(50) null, Qty decimal(15,6), ReturnQty decimal(15,6), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: ReturnId -> fcs.CnReturn.ReturnId

### fcs.CpmReceive (TABLE)
PK: Id
Columns: Id int, TransportationCode varchar(50), DeliveryDate datetime2, DistributorCode varchar(50), CreateDate datetime2 null, CreateBy varchar(20) null

### fcs.CpmReceiveDetail (TABLE)
PK: Id
Columns: Id int, ReceiveId int, TbsclDocNo varchar(50), CreateDate datetime2 null, CreateBy varchar(20) null
FK: ReceiveId -> fcs.CpmReceive.Id

### fcs.DeliveryToWarehouse (TABLE)
PK: DeliveryId
Columns: DeliveryId int, DoNo varchar(20), LoadingNo varchar(20), WarehouseCode varchar(20), Destination int null, TransporterCode varchar(50), TruckType varchar(20) null, TransactionCode varchar(20), CalculationType varchar(20), DeliveryDate datetime2, DeliveryRate varchar(20), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### fcs.DeliveryToWarehouseDetail (TABLE)
PK: DetailId
Columns: DetailId int, DeliveryId int, GtCode varchar(20), SaleCode varchar(20) null, SpecCode varchar(10) null, SizeName varchar(50) null, Qty decimal(15,6), DeliveryQty decimal(15,6), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: DeliveryId -> fcs.DeliveryToWarehouse.DeliveryId

### fcs.Destination (TABLE)
PK: DestinationId
Columns: DestinationId int, DestinationName varchar(200), Description varchar(500) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.DistributorDestinationMapping (TABLE)
PK: DestinationId, Transporter
Columns: Transporter varchar(20), DestinationId int, DistributorDestinationId int null, Remark varchar(255) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.DocsReceive (TABLE)
PK: Id
Columns: Id int, DocsReceiveTypeId int, StatusId int, FileName varchar(500), StartReadDate datetime2 null, EndReadDate datetime2 null, DocumentPeriodDate date null, TransportationCode varchar(50) null, Remark varchar(500) null, SignedBlackColorThreshold numeric(5,2) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: TransportationCode -> dbo.Transportation.TransportationCode; DocsReceiveTypeId -> fcs.DocsReceiveType.Id; StatusId -> fcs.DocsReceiveStatus.Id

### fcs.DocsReceiveDetail (TABLE)
PK: Id
Columns: Id int, DocsReceiveId int, FileName varchar(500), PageNo int, DoNo varchar(20) null, SignedFlag int null, SignedBlackColorPercentage numeric(5,2) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: DocsReceiveId -> fcs.DocsReceive.Id

### fcs.DocsReceiveStatus (TABLE)
PK: Id
Columns: Id int, Name varchar(20)

### fcs.DocsReceiveType (TABLE)
PK: Id
Columns: Id int, Name varchar(20)

### fcs.EstimateCost (TABLE)
PK: EstimateCostId
Columns: EstimateCostId bigint, TransactionCode varchar(20), RoutingID bigint null, DistributorDriverId int null, Warehouse varchar(20) null, DeliveryDate datetime2, TransactionDate datetime2 null, SystemSource varchar(20), Market varchar(20) null, CalculationType varchar(20) null, Transportation varchar(50) null, TruckType varchar(20) null, RoundType varchar(20) null, Departure int null, Destination int null, FreightRateMasterDetailId int null, PercentDiscount decimal(5,2) null, EstimateCostPerTrip decimal(18,2) null, EstimateCostPerTripTransporter decimal(18,2) null, PremiumFreightChargeId int null, PremiumFreightCharge decimal(18,2) null, PremiumFreightChargeTransporter decimal(18,2) null, ActionNote varchar(200) null, ReferenceNo varchar(50) null, SpecialChargeType varchar(20) null, NoTruckChargeFlag int null, ActiveFlag int, Reserve1 nvarchar(200) null, Reserve1Description nvarchar(200) null, Reserve2 nvarchar(200) null, Reserve2Description nvarchar(200) null, Reserve3 nvarchar(200) null, Reserve3Description nvarchar(200) null, Reserve4 nvarchar(200) null, Reserve4Description nvarchar(200) null, Reserve5 nvarchar(200) null, Reserve5Description nvarchar(200) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.EstimateCostDetail (TABLE)
PK: EstimateCostDetailId
Columns: EstimateCostDetailId bigint, EstimateCostId bigint, RoutingJobID bigint null, CustomerCode varchar(20) null, InvoiceNo varchar(20) null, TripNo varchar(20) null, ClaimNo varchar(20) null, LoadingNo varchar(20) null, DONo varchar(20) null, MasterWarehouse varchar(20) null, Departure int null, Destination int null, GTType varchar(20) null, GTCode varchar(20) null, TireCategory varchar(20) null, ItemNote varchar(200) null, Quantity int null, FreightRateMasterDetailId int null, PercentDiscount decimal(5,2) null, EstimateCostPerUnit decimal(18,2) null, EstimateCostPerUnitTransporter decimal(18,2) null, EstimateAmount decimal(18,2) null, EstimateAmountTransporter decimal(18,2) null, PremiumFreightCharge decimal(18,2) null, PremiumFreightChargeTransporter decimal(18,2) null, ActiveFlag int, Reserve1 nvarchar(200) null, Reserve1Description nvarchar(200) null, Reserve2 nvarchar(200) null, Reserve2Description nvarchar(200) null, Reserve3 nvarchar(200) null, Reserve3Description nvarchar(200) null, Reserve4 nvarchar(200) null, Reserve4Description nvarchar(200) null, Reserve5 nvarchar(200) null, Reserve5Description nvarchar(200) null, IsFreightCharge int null, IsTransporterRequest int null, MaualAdjustFlag int null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null, OriginalEstimateCostPerUnit decimal(18,2) null, OriginalEstimateCostPerUnitTransporter decimal(18,2) null, DeliveryRate int null, Reserve6 nvarchar(200) null, Reserve6Description nvarchar(200) null, Reserve7 nvarchar(200) null, Reserve7Description nvarchar(200) null, Reserve8 nvarchar(200) null, Reserve8Description nvarchar(200) null, Reserve9 nvarchar(200) null, Reserve9Description nvarchar(200) null, Reserve10 nvarchar(200) null, Reserve10Description nvarchar(200) null

### fcs.EstimateCostScheduler (TABLE)
PK: Id
Columns: Id int, ProcedureName varchar(200), RoutingId bigint null, LoadingNo varchar(20) null, DoNo varchar(20) null, DriverId int null, IsAllLoading bit, TransactionType varchar(150) null, ActionNote varchar(150) null, ItemNote varchar(150) null, CompletedSchedulerAt datetime2 null

### fcs.FinalDiscount (TABLE)
PK: FinalDiscountId
Columns: FinalDiscountId int, PaymentPeriod datetime2, DeliveryFrom datetime2 null, DeliveryTo datetime2 null, Market varchar(20) null, Transportation varchar(20), ActiveFlag int, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FinalDiscountByDeliveryDate (TABLE)
PK: FinalDiscountByDeliveryDateId
Columns: FinalDiscountByDeliveryDateId int, DiscountType varchar(20), DiscountMonth datetime2, Transportation varchar(50), DiscountRate decimal(15,2), ActiveFlag int, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FinalDiscountDetail (TABLE)
PK: FinalDiscountDetailId
Columns: FinalDiscountDetailId int, FinalDiscountId int, DiscountOrder int, DiscountType varchar(20), DiscountRate decimal(5,2), DiscountBy varchar(255), IsEffectToAmount int, IsEffectToAmountTransporter int, IsExcludeFromPercentDiscount int, ActiveFlag int, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null
FK: FinalDiscountId -> fcs.FinalDiscount.FinalDiscountId

### fcs.FinalDiscountDetailByDeliveryDate (TABLE)
PK: FinalDiscountDetailByDeliveryDateId
Columns: FinalDiscountDetailByDeliveryDateId int, FinalDiscountDetailId int, FinalDiscountByDeliveryDateId int, CreateBy varchar(20), CreateDate datetime2
FK: FinalDiscountByDeliveryDateId -> fcs.FinalDiscountByDeliveryDate.FinalDiscountByDeliveryDateId; FinalDiscountDetailId -> fcs.FinalDiscountDetail.FinalDiscountDetailId

### fcs.FreightCharge (TABLE)
PK: FreightChargeId
Columns: FreightChargeId bigint, EstimateCostId bigint null, PaymentPeriod datetime2 null, TransactionCode varchar(20) null, Warehouse varchar(20) null, DeliveryDate datetime2 null, SystemSource varchar(20) null, Market varchar(20) null, CalculationType varchar(20) null, Transportation varchar(20) null, TruckType varchar(20) null, RoundType varchar(20) null, Departure int null, Destination int null, FreightRateMasterDetailId int null, BFPercentDiscount decimal(5,2) null, BFCostPerTrip decimal(18,2) null, BFCostPerTripTransporter decimal(18,2) null, DCPercentDiscount decimal(5,2) null, DCCostPerTrip decimal(18,2) null, DCCostPerTripTransporter decimal(18,2) null, PercentDiscount decimal(5,2) null, CostPerTrip decimal(18,2) null, CostPerTripTransporter decimal(18,2) null, OldPercentDiscount decimal(5,2) null, OldCostPerTrip decimal(18,2) null, OldCostPerTripTransporter decimal(18,2) null, BFPremiumFreightCharge decimal(18,2) null, BFPremiumFreightChargeTransporter decimal(18,2) null, DCPremiumFreightCharge decimal(18,2) null, DCPremiumFreightChargeTransporter decimal(18,2) null, PremiumFreightChargeId int null, PremiumFreightCharge decimal(18,2) null, PremiumFreightChargeTransporter decimal(18,2) null, ActionNote varchar(200) null, ReferenceNo varchar(50) null, IsCassing int null, IsAlloy int null, IsTransfer int null, ActiveFlag int null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightChargeDetail (TABLE)
PK: FreightChargeDetailId
Columns: FreightChargeDetailId bigint, FreightChargeId bigint, EstimateCostDetailId bigint null, CustomerCode varchar(20) null, TripNo varchar(20) null, ClaimNo varchar(20) null, DocumentNo varchar(20) null, LoadingNo varchar(20) null, DONo varchar(20) null, Departure int null, Destination int null, MasterWarehouse varchar(20) null, GTType varchar(20) null, GTCode varchar(20) null, TireCategory varchar(20) null, ItemNote varchar(200) null, Quantity int null, FreightRateMasterDetailId int null, BFPercentDiscount decimal(5,2) null, BFCostPerUnit decimal(18,2) null, BFCostPerUnitTransporter decimal(18,2) null, BFAmount decimal(18,2) null, BFAmountTransporter decimal(18,2) null, DCPercentDiscount decimal(5,2) null, DCCostPerUnit decimal(18,2) null, DCCostPerUnitTransporter decimal(18,2) null, DCAmount decimal(18,2) null, DCAmountTransporter decimal(18,2) null, PercentDiscount decimal(5,2) null, CostPerUnit decimal(18,2) null, CostPerUnitTransporter decimal(18,2) null, Amount decimal(18,2) null, AmountTransporter decimal(18,2) null, OldPercentDiscount decimal(5,2) null, OldCostPerUnit decimal(18,2) null, OldCostPerUnitTransporter decimal(18,2) null, OldAmount decimal(18,2) null, OldAmountTransporter decimal(18,2) null, BFPremiumFreightCharge decimal(18,2) null, BFPremiumFreightChargeTransporter decimal(18,2) null, DCPremiumFreightCharge decimal(18,2) null, DCPremiumFreightChargeTransporter decimal(18,2) null, PremiumFreightCharge decimal(18,2) null, PremiumFreightChargeTransporter decimal(18,2) null, IsManualAdd int, OriginalType varchar(20) null, ActiveFlag int, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null
FK: FreightChargeId -> fcs.FreightCharge.FreightChargeId

### fcs.FreightChargeExport (TABLE)
PK: FreightChargeExportId
Columns: FreightChargeExportId bigint, InvoiceNo varchar(20), Warehouse varchar(20), LoadingDate datetime2, DocumentNo varchar(20) null, PortDestination varchar(255) null, PortChargeAmount decimal(18,2) null, SpecialChargeType varchar(20) null, AdditionalChargeAmount decimal(18,2) null, NoTruckChargeFlag int null, Remark varchar(500) null, SourceFileName varchar(100) null, ActiveFlag int, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightChargeExportDetail (TABLE)
PK: FreightChargeExportDetailId
Columns: FreightChargeExportDetailId bigint, FreightChargeExportId bigint, TruckType varchar(20), Quantity int, TruckChargeAmount decimal(18,2), StuffingChargeAmount decimal(18,2), ActiveFlag int, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightChargeExportReturnContainer (TABLE)
PK: ReturnContainerId
Columns: ReturnContainerId bigint, FreightChargeExportId bigint, ReturnContainerPlace varchar(20) null, ReturnTruckCharge decimal(18,2) null, ReturnContainerQuantity int null, ReturnTruckChargeAmount decimal(18,2) null, ActiveFlag int, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightPeriodClose (TABLE)
PK: FreightPeriodCloseId
Columns: FreightPeriodCloseId bigint, PaymentPeriod datetime2 null, ClosePeriodFlag int null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateClaimMaster (TABLE)
PK: FreightRateClaimMasterId
Columns: FreightRateClaimMasterId int, Departure int, Destination int, Distance decimal(18,2), CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateClaimMasterDetail (TABLE)
PK: FreightRateClaimMasterDetailId
Columns: FreightRateClaimMasterDetailId int, FreightRateClaimMasterId int, TireCategory varchar(20), ChargeRate decimal(18,2), ChargeRateTransporter decimal(18,2), EffectiveDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateMatTMaster (TABLE)
PK: FreightRateMatTMaster
Columns: FreightRateMatTMaster int, RoundType varchar(20), Transportation varchar(20) null, Departure int, Destination int, Distance decimal(18,2) null, PercentDiscount decimal(5,2), ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateStandard (TABLE)
PK: StandardRateId
Columns: StandardRateId int, Market varchar(20), Transportation varchar(20), Departure int, Destination int, Distance decimal(18,2) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateStandardDetail (TABLE)
PK: StandardRateDetailId
Columns: StandardRateDetailId int, StandardRateId int, PercentDiscount decimal(5,2) null, GTType varchar(20), TireCategory varchar(20), ChargeRate decimal(18,2), ChargeRateTransporter decimal(18,2), EffectiveDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateTripMaster (TABLE)
PK: FreightRateMasterTripId
Columns: FreightRateMasterTripId int, Market varchar(20), Transportation varchar(20), Departure int, Destination int, Distance decimal(18,2) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateTripMasterDetail (TABLE)
PK: FreightRateMasterTripDetailId
Columns: FreightRateMasterTripDetailId int, FreightRateMasterTripId int, TruckType varchar(20), PercentDiscount decimal(5,2) null, ChargeRate decimal(18,2), ChargeRateTransporter decimal(18,2), EffectiveDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateUnitMaster (TABLE)
PK: FreightRateMasterUnitId
Columns: FreightRateMasterUnitId int, Market varchar(20), Transportation varchar(20), Departure int, Destination int, Distance decimal(18,2) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.FreightRateUnitMasterDetail (TABLE)
PK: FreightRateMasterUnitDetailId
Columns: FreightRateMasterUnitDetailId int, FreightRateMasterUnitId int, PercentDiscount decimal(5,2) null, GTType varchar(20), TireCategory varchar(20), ChargeRate decimal(18,2), ChargeRateTransporter decimal(18,2), EffectiveDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.ImportReceiveDoDetail (TABLE)
PK: Id
Columns: Id int, DocsReceiveId int, FileName varchar(500), PageNo int, DoNo varchar(20) null, SignedFlag int, SignedBlackColorPercentage numeric(5,2), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: DocsReceiveId -> fcs.DocsReceive.Id

### fcs.ImportTransferSlipDetail (TABLE)
PK: Id
Columns: Id int, DocsReceiveId int, FileName varchar(500), PageNo int, TripNo varchar(20) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: DocsReceiveId -> fcs.DocsReceive.Id

### fcs.OriginalDOMapping (TABLE)
PK: DONo
Columns: DONo varchar(20), OriginalDONo varchar(20) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.OverLimitDropsChargeRate (TABLE)
PK: Id
Columns: Id int, TransportationCode varchar(50), LimitDrops int, ChargeRate decimal(18,2), ChargeRateTransporter decimal(18,2), EffectiveDate datetime2, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.PortDestination (TABLE)
PK: InvoiceNo
Columns: InvoiceNo varchar(50), PortDestination varchar(255) null, SourceFileName varchar(100), CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.PremiumFreightRateMaster (TABLE)
PK: PremiumFreightRateMasterId
Columns: PremiumFreightRateMasterId int, Transportation varchar(20) null, Departure int null, Destination int, Distance decimal(18,2) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.PremiumFreightRateMasterDetail (TABLE)
PK: PremiumFreightRateMasterDetailId
Columns: PremiumFreightRateMasterDetailId int, PremiumFreightRateMasterId int, TruckType varchar(20), PercentDiscount decimal(5,2), PremiumFreightRate decimal(18,2), PremiumFreightRateTransport decimal(18,2), EffectiveDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.ReturnContainerPlaceMaster (TABLE)
PK: ReturnContainerPlaceMaster
Columns: ReturnContainerPlaceMaster int, ReturnContainerPlace varchar(20), ChargeAmount decimal(18,2), EffectiveDate datetime2, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.SpecialChargeMaster (TABLE)
PK: SpecialChargeType, TruckType
Columns: SpecialChargeType varchar(20), TruckType varchar(20), ChargeRate decimal(18,2) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.StuffingChargeMaster (TABLE)
PK: StuffingChargeMasteId
Columns: StuffingChargeMasteId int, Warehouse varchar(20), TruckType varchar(20), MinNum int, MaxNum int null, ChargeAmount decimal(18,2), EffectiveDate datetime2 null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### fcs.TransportationMapping (TABLE)
PK: MappingId
Columns: MappingId int, TextData varchar(50) null, TransportationCode varchar(50) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### fcs.TransportationRequest (TABLE)
PK: TransportationRequestId
Columns: TransportationRequestId int, Transportation varchar(50), DocumentPeriod datetime2, ActiveFlag int, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: Transportation -> dbo.Transportation.TransportationCode

### fcs.TransportationRequestDetail (TABLE)
PK: TransportationRequestDetailId
Columns: TransportationRequestDetailId int, TransportationRequestId int, EstimateCostId int null, ReferenceNo varchar(20) null, LoadingNo varchar(20) null, DONo varchar(20) null, RequestAmount decimal(18,2) null, ActiveFlag int, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20) null, UpdateDate datetime2 null

### fcs.TruckChargeMaster (TABLE)
PK: TruckContainerChargeMasterId
Columns: TruckContainerChargeMasterId int, Warehouse varchar(20), TruckType varchar(20), ChargeAmount decimal(18,2), EffectiveDate datetime2, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### honda.CustomerHonda (TABLE)
PK: CustomerCode
Columns: CustomerCode varchar(20), CreatedDate datetime2 null, CreatedBy varchar(20) null

### import.ImportDelayReason (TABLE)
PK: ImportID
Columns: ImportID int, StoreFileName varchar(100), UploadFileName varchar(100), CreatedAt datetime2, CreatedBy varchar(20)

### import.ImportDelayReasonDetail (TABLE)
PK: ImportDetailID
Columns: ImportDetailID int, ImportID int, Warehouse varchar(20) null, Transporter varchar(100) null, LoadingNo varchar(20), DoNo varchar(20), CustomerName varchar(150) null, ApproveDate datetime2 null, PlanDeliveryDate datetime2 null, ReasonId int, ReasonName varchar(200)
FK: ImportID -> import.ImportDelayReason.ImportID

### import.ImportDistributorReceive (TABLE)
PK: ImportID
Columns: ImportID int, TransportationCode varchar(50), StoreFileName varchar(100), UploadFileName varchar(100), ConfirmedAt datetime2 null, CreatedAt datetime2, CreatedBy varchar(20)
FK: TransportationCode -> dbo.Transportation.TransportationCode

### import.ImportDistributorReceiveDetail (TABLE)
PK: ImportDetailID
Columns: ImportDetailID int, ImportID int, TransportDate datetime2, TransportNo varchar(20), ReceivedDate datetime2, ReceivedTime time, Latitude numeric(12,9) null, Longitude numeric(12,9) null
FK: ImportID -> import.ImportDistributorReceive.ImportID

### import.ImportDoReceive (TABLE)
PK: ImportId
Columns: ImportId int, TransportationCode varchar(50), StoreFileName varchar(100), UploadFileName varchar(100), ConfirmedAt datetime2 null, CreatedAt datetime2, CreatedBy varchar(20)
FK: TransportationCode -> dbo.Transportation.TransportationCode

### import.ImportDoReceiveDetail (TABLE)
PK: ImportDetailId
Columns: ImportDetailId int, ImportId int, TransportNo varchar(20), ReceivedDate datetime2, ReceivedTime time, Latitude numeric(12,9) null, Longitude numeric(12,9) null
FK: ImportId -> import.ImportDoReceive.ImportId

### import.ImportFreightChargeExport (TABLE)
PK: ImportID
Columns: ImportID int, StoreFileName varchar(100), UploadFileName varchar(100), CreatedAt datetime2, CreatedBy varchar(20)

### import.ImportFreightChargeExportDetail (TABLE)
PK: ImportDetailID
Columns: ImportDetailID int, ImportID int, InvoiceNo varchar(20), LoadingAt varchar(20) null, WarehouseCode varchar(20) null, WarehouseName varchar(20) null, DocumentNo varchar(20) null, PortDestination varchar(255) null, PortChargeAmount decimal(18,2) null, ReturnContainerPlace varchar(20) null, ReturnContainerPlaceName varchar(20) null, ReturnContainerQuantity int null, Remark varchar(255) null, SpecialChargeType varchar(20) null
FK: ImportID -> import.ImportFreightChargeExport.ImportID

### import.ImportFreightRateStandard (TABLE)
PK: ImportId
Columns: ImportId int, StoreFileName varchar(100) null, UploadFileName varchar(100) null, ConfirmedAt datetime2 null, CreatedDate datetime2 null, CreatedBy varchar(20) null

### import.ImportFreightRateStandardDetail (TABLE)
PK: DetailId
Columns: DetailId int, ImportId int null, StandardRateId int null, StandardRateDetailId int null, MarketName varchar(100) null, MarketCode varchar(20) null, TransporterName varchar(200) null, TransporterCode varchar(20) null, DepartureName varchar(200) null, DepartureId int null, DestinationName varchar(200) null, DestinationId int null, Distance decimal(18,2) null, GtTypeName varchar(100) null, GtTypeCode varchar(20) null, TireCategoryName varchar(100) null, TireCategoryCode varchar(20) null, PercentDiscount decimal(5,2) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, ErrorMessage varchar(500) null
FK: ImportId -> import.ImportFreightRateStandard.ImportId

### import.ImportFreightRateTrip (TABLE)
PK: ImportId
Columns: ImportId int, StoreFileName varchar(100) null, UploadFileName varchar(100) null, ConfirmedAt datetime2 null, CreatedDate datetime2 null, CreatedBy varchar(20) null

### import.ImportFreightRateTripDetail (TABLE)
PK: DetailId
Columns: DetailId int, ImportId int null, FreightRateMasterTripId int null, FreightRateMasterTripDetailId int null, MarketName varchar(100) null, MarketCode varchar(20) null, TransporterName varchar(200) null, TransporterCode varchar(20) null, DepartureName varchar(200) null, DepartureId int null, DestinationName varchar(200) null, DestinationId int null, Distance decimal(18,2) null, TruckTypeName varchar(100) null, TruckTypeCode varchar(20) null, PercentDiscount decimal(5,2) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, ErrorMessage varchar(500) null
FK: ImportId -> import.ImportFreightRateTrip.ImportId

### import.ImportFreightRateUnit (TABLE)
PK: ImportId
Columns: ImportId int, StoreFileName varchar(100) null, UploadFileName varchar(100) null, ConfirmedAt datetime2 null, CreatedDate datetime2 null, CreatedBy varchar(20) null

### import.ImportFreightRateUnitDetail (TABLE)
PK: DetailId
Columns: DetailId int, ImportId int null, FreightRateMasterUnitId int null, FreightRateMasterUnitDetailId int null, MarketName varchar(100) null, MarketCode varchar(20) null, TransporterName varchar(200) null, TransporterCode varchar(20) null, DepartureName varchar(200) null, DepartureId int null, DestinationName varchar(200) null, DestinationId int null, Distance decimal(18,2) null, GtTypeName varchar(100) null, GtTypeCode varchar(20) null, TireCategoryName varchar(100) null, TireCategoryCode varchar(20) null, PercentDiscount decimal(5,2) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, ErrorMessage varchar(500) null
FK: ImportId -> import.ImportFreightRateUnit.ImportId

### import.ImportHoliday (TABLE)
PK: ImportId
Columns: ImportId bigint, ImportYear int null, StoreFilename varchar(100) null, UploadFilename varchar(100) null, ConfirmedAt datetime2 null, CreatedAt datetime2 null, CreatedBy varchar(20) null

### import.ImportHolidayDetail (TABLE)
PK: Id
Columns: Id bigint, ImportId bigint null, HolidayDate datetime2, HolidayName varchar(255) null
FK: ImportId -> import.ImportHoliday.ImportId

### import.ImportPlan (TABLE)
PK: BatchNo, RecordNo
Columns: BatchNo varchar(50), RecordNo int, LoadingNo varchar(20) null, SoNo varchar(20) null, DoNo varchar(20) null, DoNoInFile varchar(20) null, SuffixNo varchar(20) null, SaleCode varchar(20) null, InvoiceNo varchar(20) null, DocNo varchar(20) null, DoLine varchar(20) null, CustomerCode varchar(20) null, CustomerName varchar(150) null, ShipTo varchar(255) null, Province varchar(20) null, Market varchar(20) null, Warehouse varchar(20) null, MasterWarehouse varchar(20) null, GtCode varchar(20) null, Grade varchar(20) null, SpecCode varchar(50) null, Bf varchar(20) null, Sizename varchar(50) null, TranQty numeric(18,4) null, BstlCode varchar(20) null, ProductionDate datetime2 null, LoadCreateDate datetime2 null, DeliveryDate datetime2 null, PlanDate datetime2 null, PickingStartTime datetime2 null, PickingFinishTime datetime2 null, TruckArrivedTime datetime2 null, LoadingStartTime datetime2 null, LoadingFinishTime datetime2 null, DeliveryTime datetime2 null, TransportCompay varchar(20) null, TruckSize varchar(20) null, LicensePlate varchar(100) null, LoadRemark varchar(100) null, ImportDate datetime2 null, ImportBy varchar(20) null, SkipFifoFlag int null

### import.ImportPlanning (TABLE)
PK: ImportId
Columns: ImportId int, StoreFileName varchar(100) null, UploadFileName varchar(100) null, ConfirmedAt datetime2 null, CreatedDate datetime2 null, CreatedBy varchar(20) null

### import.ImportPlanningDetail (TABLE)
PK: DetailId
Columns: DetailId int, ImportId int null, RecordNo int null, LoadingNo varchar(20) null, SoNo varchar(20) null, DoNo varchar(20) null, DoNoInFile varchar(20) null, SuffixNo varchar(20) null, SaleCode varchar(20) null, InvoiceNo varchar(20) null, DocNo varchar(20) null, DoLine varchar(20) null, CustomerCode varchar(20) null, CustomerName varchar(150) null, ShipTo varchar(255) null, Province varchar(20) null, Market varchar(20) null, Warehouse varchar(20) null, MasterWarehouse varchar(20) null, GtCode varchar(20) null, Grade varchar(20) null, SpecCode varchar(50) null, Bf varchar(20) null, Sizename varchar(50) null, TranQty numeric(18,4) null, BstlCode varchar(20) null, ProductionDate datetime2 null, LoadCreateDate datetime2 null, DeliveryDate datetime2 null, PlanDate datetime2 null, PickingStartTime datetime2 null, PickingFinishTime datetime2 null, TruckArrivedTime datetime2 null, LoadingStartTime datetime2 null, LoadingFinishTime datetime2 null, DeliveryTime datetime2 null, TransportCompany varchar(50) null, TruckSize varchar(20) null, LicensePlate varchar(100) null, LoadRemark varchar(100) null, ImportDate datetime2 null, ImportBy varchar(20) null, SkipFifoFlag int null, ErrorCode varchar(200) null, TireCheckerPlanType varchar(100) null
FK: ImportId -> import.ImportPlanning.ImportId

### import.ImportPremiumFreightRate (TABLE)
PK: ImportId
Columns: ImportId int, StoreFileName varchar(100) null, UploadFileName varchar(100) null, ConfirmedAt datetime2 null, CreatedDate datetime2 null, CreatedBy varchar(20) null

### import.ImportPremiumFreightRateDetail (TABLE)
PK: DetailId
Columns: DetailId int, ImportId int null, PremiumFreightRateMasterId int null, PremiumFreightRateMasterDetailId int null, TransporterName varchar(200) null, TransporterCode varchar(20) null, DepartureName varchar(200) null, DepartureId int null, DestinationName varchar(200) null, DestinationId int null, Distance decimal(18,2) null, TruckTypeName varchar(100) null, TruckTypeCode varchar(20) null, PercentDiscount decimal(5,2) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, ErrorMessage varchar(500) null
FK: ImportId -> import.ImportPremiumFreightRate.ImportId

### import.ImportWarehouseTask (TABLE)
PK: ImportId
Columns: ImportId int, StoreFileName varchar(100) null, UploadFileName varchar(100) null, ConfirmedAt datetime2 null, ImportDate datetime2 null, ImportBy varchar(20) null

### import.ImportWarehouseTaskDetail (TABLE)
PK: ImportDetailId
Columns: ImportDetailId int, ImportId int null, RecordNo int null, WarehouseTask varchar(20) null, DocumentNo varchar(20) null, WarehouseOrder varchar(20) null, WTItem varchar(20) null, HUWarehouseTask varchar(20) null, WhseProcessType varchar(20) null, WhseProcessCategory varchar(20) null, WhseProcessCategoryDesc varchar(50) null, Activity varchar(20) null, StorageProcess varchar(20) null, ExternalProcessStep varchar(20) null, WarehouseTaskStatus varchar(20) null, CreatedBy varchar(20) null, CreatedOn date null, CreatedAt time null, ConfirmedBy varchar(20) null, ConfirmationDate date null, ConfirmationTime time null, MovementReason varchar(50) null, ExceptionCode varchar(20) null, StartDate date null, StartTime time null, Product varchar(20) null, ProductShortDescription varchar(100) null, BatchNo varchar(20) null, StockType varchar(20) null, DescriptionStockType varchar(100) null, Type varchar(20) null, SalesOrderProject varchar(20) null, SalesOrderItem varchar(20) null, DocumentCategory varchar(20) null, Usage varchar(20) null, Owner varchar(20) null, PartnerRole varchar(20) null, SrcTrgtQtyBUom int null, ActQtyDestBUom int null, DestDiffQtyInBum int null, BaseUnitOfMeasure varchar(20) null, SrcTrgtQtyAUoM int null, ActDestQtyAltUoM int null, DiffQtyInAltUn int null, AltUnitOfMeasure varchar(20) null, ActualQuantityInVUM int null, ValuationUnit varchar(20) null, ValuationMeasured varchar(20) null, DifferenceQuantityVUM int null, ExactDiffStatusInCW varchar(20) null, HandlingUnitType varchar(20) null, HazardRating1 varchar(20) null, HazardRating2 varchar(20) null, LoadingWeight decimal(18,4) null, WeightUnit varchar(20) null, LoadingVolume decimal(18,4) null, VolumeUnit varchar(20) null, CapacityConsumption decimal(18,4) null, PlndProcTimeWT int null, TimeUnit varchar(20) null, InventoryOnPutawayPlanned varchar(20) null, PutawayPhysInventory varchar(20) null, LowStockCheckPlanned varchar(20) null, LowStockCheck varchar(20) null, ShelfLifeExpirationDate date null, GoodsReceiptDate date null, GRProcessingTime time null, CountryofOrigin varchar(20) null, HazardousSubstanceRelevantForStorage varchar(100) null, InspectionType varchar(20) null, QualityInspection int null, TargetStockID varchar(50) null, SourceStorageType varchar(20) null, SourceStorSection varchar(20) null, SourceStorageBin varchar(20) null, SourceResource varchar(20) null, InternalSourceTU varchar(20) null, SourceTransportationUnit varchar(20) null, SourceCarrier varchar(20) null, SourceLocationType varchar(20) null, SourceHandlingUnit varchar(20) null, RetentionQuantity int null, DestStorageType varchar(20) null, DestStorSection varchar(20) null, DestinationBin varchar(20) null, DestResource varchar(20) null, IntDestinationTU varchar(20) null, DestinationTU varchar(20) null, DestinationCarrier varchar(20) null, DestLocationType varchar(20) null, LoadingNo varchar(20) null, Warehouse varchar(20) null, Market varchar(20) null, GtCode varchar(20) null, GtCodeNew varchar(20) null, ErrorFlag int null, ErrorDescription varchar(20) null, ImportDate datetime2 null, ImportBy varchar(20) null
FK: ImportId -> import.ImportWarehouseTask.ImportId

### logs.BsProductionWeek (TABLE)
PK: LogId
Columns: LogId int, ProductionDate date null, ProductionWeek varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.CustomerDayoffLog (TABLE)
PK: LogID
Columns: LogID int, CustomerCode varchar(20) null, Day varchar(20) null, Dayofweek int null, Closed int null, SectionFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.CustomerDistanceLog (TABLE)
PK: LogId
Columns: LogId int, CustomerCode varchar(20) null, WarehouseCode varchar(20) null, Distance numeric(18,4) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.CustomerLog (TABLE)
PK: LogId
Columns: LogId int, CustomerCode varchar(20) null, CustomerName varchar(150) null, CustomerGroupCode varchar(20) null, Address varchar(255) null, ProvinceCode int null, Email varchar(150) null, Mobile varchar(150) null, DeliveryLeadTime int null, TiresAgeAcceptance int null, Market varchar(20) null, ShipmentType2D int null, Latitude float null, Longitude float null, FcsDestination int null, MultipleDestinationFlag int null, TimespanOe int null, RequireSprFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, RoutingType varchar(20) null, Note varchar(max) null, ZoneID int null, CustomerLocationFlag int null, CustomerType varchar(20) null, Postcode varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.DelayTimeLog (TABLE)
PK: LogId
Columns: LogId int, ReasonCode nvarchar(20) null, ReasonName nvarchar(255) null, DelayTime int null, IsActive bit null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.DestinationLog (TABLE)
PK: LogId
Columns: LogId int, DestinationId int null, DestinationName varchar(200) null, Description varchar(500) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.ETACalculationLog (TABLE)
PK: LogId
Columns: LogId bigint, CalculationDateTime datetime2 null, Channel varchar(50) null, RoutingID bigint null, DistributorRoutingID bigint null, LoadingNo varchar(20) null, DONo varchar(50) null, DistributorFlag int null, DistributorCode varchar(50) null, TruckDepartDate datetime2 null, DistributorDepartDate datetime2 null, LeadTimeToCustomer int null, LeadTimeToDistributor int null, LeadTimeDistributorToCustomer int null, ETATruckDepart datetime2 null, ETADistributorDepart datetime2 null, DelayTime int null, DistributorDelayTime int null, ETAFinal datetime2 null, ProcessDate datetime2 null, ProcessBy varchar(128) null

### logs.FreightRateClaimMasterDetailLog (TABLE)
PK: LogId
Columns: LogId int, FreightRateClaimMasterDetailId int null, FreightRateClaimMasterId int null, TireCategory varchar(20) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateClaimMasterLog (TABLE)
PK: LogId
Columns: LogId int, FreightRateClaimMasterId int null, Departure int null, Destination int null, Distance decimal(18,2) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateMatTMasterLog (TABLE)
PK: LogId
Columns: LogId int, FreightRateMatTMaster int null, RoundType varchar(20) null, Transportation varchar(20) null, Departure int null, Destination int null, Distance decimal(18,2) null, PercentDiscount decimal(5,2) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateStandardDetailLog (TABLE)
PK: LogId
Columns: LogId int, StandardRateDetailId int null, StandardRateId int null, PercentDiscount decimal(5,2) null, GTType varchar(20) null, TireCategory varchar(20) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateStandardLog (TABLE)
PK: LogId
Columns: LogId int, StandardRateId int null, Market varchar(20) null, Transportation varchar(20) null, Departure int null, Destination int null, Distance decimal(18,2) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateTripMasterDetailLog (TABLE)
PK: LogId
Columns: LogId int, FreightRateMasterTripDetailId int null, FreightRateMasterTripId int null, TruckType varchar(20) null, PercentDiscount decimal(5,2) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateTripMasterLog (TABLE)
PK: LogId
Columns: LogId int, FreightRateMasterTripId int null, Market varchar(20) null, Transportation varchar(20) null, Departure int null, Destination int null, Distance decimal(18,2) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateUnitMasterDetailLog (TABLE)
PK: LogId
Columns: LogId int, FreightRateMasterUnitDetailId int null, FreightRateMasterUnitId int null, PercentDiscount decimal(5,2) null, GTType varchar(20) null, TireCategory varchar(20) null, ChargeRate decimal(18,2) null, ChargeRateTransporter decimal(18,2) null, EffectiveDate datetime2 null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.FreightRateUnitMasterLog (TABLE)
PK: LogId
Columns: LogId int, FreightRateMasterUnitId int null, Market varchar(20) null, Transportation varchar(20) null, Departure int null, Destination int null, Distance decimal(18,2) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.ImportDelayReasonLog (TABLE)
PK: ImportId
Columns: ImportId int, Filename varchar(200), SystemFilename varchar(200), CreateDate datetime2, CreateBy varchar(20)

### logs.LtDeliveryBsToCustomerLog (TABLE)
PK: LogId
Columns: LogId int, FcsWarehouseCode varchar(20) null, CustomerCode varchar(20) null, DeliveryMethod varchar(20) null, LeadTimeHour int null, CreatedDate datetime2 null, CreatedBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.LtDeliveryBsToDistributorLog (TABLE)
PK: LogId
Columns: LogId int, FcsWarehouseCode varchar(20) null, DeliveryMethod varchar(20) null, LeadTimeHour int null, CreatedDate datetime2 null, CreatedBy varchar(50) null, UpdateDate datetime2 null, UpdateBy varchar(50) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.LtDeliveryDistributorToCustomerLog (TABLE)
PK: LogId
Columns: LogId int, CustomerCode varchar(20) null, DeliveryMethod varchar(20) null, LeadTimeHour int null, CreatedDate datetime null, CreatedBy varchar(20) null, UpdateDate datetime null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.MasterModify (TABLE)
PK: LogId
Columns: LogId int, MasterType varchar(20) null, UserType varchar(20) null, BusinessKey1 varchar(20) null, BusinessKey2 varchar(20) null, BusinessKey3 varchar(20) null, BusinessKey4 varchar(20) null, BusinessKey5 varchar(20) null, Description varchar(255) null, ActionDate datetime2 null, ActionBy varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### logs.PremiumFreightRateMasterDetailLog (TABLE)
PK: LogId
Columns: LogId int, PremiumFreightRateMasterDetailId int null, PremiumFreightRateMasterId int null, TruckType varchar(20) null, PercentDiscount decimal(5,2) null, PremiumFreightRate decimal(18,2) null, PremiumFreightRateTransport decimal(18,2) null, EffectiveDate datetime2 null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.PremiumFreightRateMasterLog (TABLE)
PK: LogId
Columns: LogId int, PremiumFreightRateMasterId int null, Transportation varchar(20) null, Departure int null, Destination int null, Distance decimal(18,2) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.ReturnContainerPlaceMasterLog (TABLE)
PK: LogId
Columns: LogId int, ReturnContainerPlaceMaster int null, ReturnContainerPlace varchar(20) null, ChargeAmount decimal(18,2) null, EffectiveDate datetime2 null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, EventLog varchar(50) null, EventDate datetime2 null

### logs.SapDeliveryOrderLog (TABLE)
PK: Id
Columns: Id int, DeliveryOrderImport int null, StoredFilePath varchar(500), TotalLine int, TotalSkipLine int, ErrorMessage varchar(500) null
FK: DeliveryOrderImport -> sap.DeliveryOrderImport.Id

### logs.StickerMasterLog (TABLE)
PK: LogId
Columns: LogId int, StickerCode varchar(20) null, Warehouse varchar(20) null, StickerBarcode varchar(50) null, MainStockLocation varchar(150) null, SpareStockLocation varchar(150) null, ReorderPoint int null, MaximumPoint int null, StickerImg varchar(255) null, BarcodeScanFlag int null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.StuffingChargeMasterLog (TABLE)
PK: LogId
Columns: LogId int, StuffingChargeMasteId int null, Warehouse varchar(20) null, TruckType varchar(20) null, MinNum int null, MaxNum int null, ChargeAmount decimal(18,2) null, EffectiveDate datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.TiresCheckerSapResult (TABLE)
PK: LoadingNo, DONo
Columns: LoadingNo varchar(20), DONo varchar(20), ResponseCode varchar(20) null, ResponseBody nvarchar(max) null, TraceLogId bigint, LastUpdated datetime2

### logs.TiresMasterLog (TABLE)
PK: LogId
Columns: LogId int, TiresCode varchar(20) null, Warehouse varchar(20) null, GTCode varchar(20) null, SaleCode varchar(20) null, SizeCode varchar(20) null, SizeName varchar(50) null, TiresSize varchar(20) null, StructureCode varchar(20) null, BPCPlus varchar(20) null, GTType varchar(20) null, JigType varchar(20) null, JigCode varchar(20) null, PartTag varchar(20) null, WeightStd numeric(15,6) null, Capacity numeric(15,6) null, TireBrand varchar(20) null, DomesticType varchar(20) null, BookQuantity int null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.TiresStickerLog (TABLE)
PK: LogId
Columns: LogId int, TiresMasterID bigint null, TiresCode varchar(20) null, StickerCode varchar(20) null, StickerMarket varchar(20) null, StickerCust varchar(20) null, CustomerGroupCode varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.TransportationLog (TABLE)
PK: LogId
Columns: LogId int, TransportationCode varchar(50) null, TransportationName varchar(200) null, TransportationFullName varchar(200) null, FcsTransporterCode varchar(20) null, GroupCode varchar(20) null, PercentDiscount decimal(5,2) null, DistributorFlag int null, Destination int null, DefaultDriverId int null, FreightDisplayFlag int null, ActiveFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.TransportationPlateLog (TABLE)
PK: LogId
Columns: LogId int, TransportationPlateID int null, TransportationCode varchar(50) null, TruckTypeCode varchar(50) null, PlateNo varchar(10) null, PlateProvinceCode varchar(10) null, Capacity decimal(15,6) null, NetWeight decimal(15,6) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.TransportationRouteLog (TABLE)
PK: LogId
Columns: LogId int, ZoneID int null, TransportationCode varchar(50) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.TreadMarkImageLog (TABLE)
PK: LogId
Columns: LogId int, Id int null, TiresCode varchar(20) null, ImagePath varchar(255) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.Trip (TABLE)
Columns: TripNo varchar(20) null, TripType varchar(20) null, TransferType varchar(20) null, DepartureDate datetime null, DepartureFrom varchar(20) null, ArrivedDate datetime null, ArrivedTo varchar(20) null, TransporterCode varchar(20) null, LicensePlate varchar(20) null, DriverName varchar(50) null, TruckType varchar(20) null, RefTripNo varchar(20) null, TruckEmptyFlag int, CancelTrip int null, SecurityDepartDate datetime null, SecurityDepartStatus varchar(20) null, SecurityDepartNote varchar(255) null, SecurityArrivedDate datetime null, SecurityArrivedStatus varchar(20) null, SecurityArrivedNote varchar(255) null, CreateDate datetime null, CreateBy nvarchar(255) null, UpdateDate datetime null, UpdateBy nvarchar(255) null

### logs.TruckChargeMasterLog (TABLE)
PK: LogId
Columns: LogId int, TruckContainerChargeMasterId int null, Warehouse varchar(20) null, TruckType varchar(20) null, ChargeAmount decimal(18,2) null, EffectiveDate datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### logs.WmsLog (TABLE)
PK: Id
Columns: Id bigint, WmsLogTypeId int, LoadingNo varchar(20) null, RequestBody nvarchar(max) null, ResponseBody nvarchar(max) null, ResponseStatusCode int, ExceptionType varchar(200) null, ExceptionMessage nvarchar(500) null, CreatedAt datetime2, CreatedBy varchar(20)
FK: WmsLogTypeId -> logs.WmsLogType.WmsLogTypeId

### logs.WmsLogType (TABLE)
PK: WmsLogTypeId
Columns: WmsLogTypeId int, Name varchar(200), CreatedAt datetime2, CreatedBy varchar(20)

### logs.ZoneLog (TABLE)
PK: LogId
Columns: LogId int, ZoneID int null, ParentZoneID int null, ZoneCode varchar(100) null, ZoneName varchar(100) null, ZoneLevel int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, EventLog varchar(50) null, EventDate datetime2 null

### masupport.fixStartLoading (TABLE)
PK: RunNo
Columns: RunNo int, FixTime datetime null, LoadingNo varchar(20) null, StartScan datetime null, ScanBy varchar(20) null

### masupport.ReceiveItem (TABLE)
Columns: ReceiveId int, Warehouse varchar(20), ReceiveType varchar(20) null, QrCode varchar(200) null, TagNo varchar(50) null, LoadingNo varchar(20) null, ReturnDocument varchar(20) null, ReceiveDate datetime2, ReceiveBy varchar(20), Qty int, CutOffDate date, ReceiveSubType varchar(20) null, EwmTag varchar(100) null, MappingCompleteFlag int null, MappingCompleteDate datetime2 null, Address varchar(20) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### masupport.ReceiveItemDetail (TABLE)
Columns: ReceiveDetailId int, ReceiveId int, SerialNo varchar(20), GtCode varchar(20), Grade varchar(20), ProductionDate datetime2, ProductionWeek varchar(20), ScanDate datetime2, ScanBy varchar(20), ReturnFlag int null, TcFlag int null, TsgFlag int null, DocumentNo varchar(20) null, SerialId int null, ReceiveType varchar(20) null, ReworkFlag int null, ManualWeekFlag int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### masupport.SplitEstimateCost (TABLE)
PK: RunNo
Columns: RunNo int, EstimateCostId int null, SplitEstimateCostId int null, SplitDate datetime null

### mobile.DeleteRouting (TABLE)
Columns: RoutingID bigint, UserID int, ActiveFlag int, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20), DeleteDate datetime2, DeleteBy varchar(20)

### mobile.DeleteRoutingJob (TABLE)
Columns: RoutingJobID bigint, RoutingID bigint, DONo varchar(20), LoadingNo varchar(20), ScanDate datetime2, CompleteFlag int, NextDropFlag int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20), DeleteDate datetime2, DeleteBy varchar(20)

### mobile.DeliveryComplete (TABLE)
PK: DeliveryId
Columns: DeliveryId int, LoadingNo varchar(20) null, DONo varchar(20) null, DriverId int null, CompleteReason varchar(20) null, SendCompleteToNSSFlag int null, SendCompleteToNSSAt datetime2 null, CreateDate datetime2 null, CreateBy varchar(20) null

### mobile.DeliveryDelayDO (TABLE)
PK: Id
Columns: Id int, DeliveryDelayReasonId int null, LoadingNo varchar(20) null, DONo varchar(20) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20), UpdateDate datetime2

### mobile.DeliveryDelayReason (TABLE)
PK: DeliveryDelayReasonId
Columns: DeliveryDelayReasonId int, UserID int, RoutingID bigint, ReasonCode nvarchar(20) null, Remark nvarchar(500) null, DelayTime int null, Latitude float null, Longitude float null, RecordedReasonAt datetime2, SendDelayToNSSFlag int null, SendDelayToNSSAt datetime2 null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20), UpdateDate datetime2
FK: UserID -> dbo.UserLogin.UserID; ReasonCode -> dbo.DelayTime.ReasonCode

### mobile.DistributorDelayDO (TABLE)
PK: Id
Columns: Id int, DistributorDelayReasonId int null, LoadingNo varchar(20) null, DONo varchar(20) null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20), UpdateDate datetime2

### mobile.DistributorDelayReason (TABLE)
PK: DistributorDelayReasonId
Columns: DistributorDelayReasonId int, UserID int, DistributorRoutingID bigint, ReasonCode nvarchar(20) null, Remark nvarchar(500) null, DelayTime int null, Latitude float null, Longitude float null, RecordedReasonAt datetime2, SendDelayToNSSFlag int null, SendDelayToNSSAt datetime2 null, CreateBy varchar(20), CreateDate datetime2, UpdateBy varchar(20), UpdateDate datetime2
FK: ReasonCode -> dbo.DelayTime.ReasonCode

### mobile.DistributorLocationTrackingLog (TABLE)
PK: Id
Columns: Id bigint, DistrubotorRoutingID bigint, UserID int, VisitorId nvarchar(250) null, Latitude float, Longitude float, CreatedAt datetime

### mobile.DistributorRouting (TABLE)
PK: DistributorRoutingID
Columns: DistributorRoutingID bigint, UserID int, ActiveFlag int, Latitude float null, Longitude float null, UpdateLocationDate datetime2 null, LicensePlate varchar(100) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### mobile.DistributorRoutingJob (TABLE)
PK: DistributorRoutingJobID
Columns: DistributorRoutingJobID bigint, DistributorRoutingID bigint, DONo varchar(20), LoadingNo varchar(20), Latitude float null, Longitude float null, ScanDate datetime2, NextDropFlag int null, CompleteFlag int, ReturnFlag int null, ReturnCompleteFlag int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Plan.LoadingNo; DistributorRoutingID -> mobile.DistributorRouting.DistributorRoutingID

### mobile.LocationTrackingLog (TABLE)
PK: Id
Columns: Id int, UserID int, VisitorId nvarchar(250) null, Latitude float, Longitude float, CreatedAt datetime

### mobile.Routing (TABLE)
PK: RoutingID
Columns: RoutingID bigint, UserID int, ActiveFlag int, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### mobile.RoutingJob (TABLE)
PK: RoutingJobID
Columns: RoutingJobID bigint, RoutingID bigint, DONo varchar(20), LoadingNo varchar(20), ScanDate datetime2, NextDropFlag int null, CompleteFlag int, ReturnFlag int null, ReturnCompleteFlag int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Plan.LoadingNo; RoutingID -> mobile.Routing.RoutingID

### mobile.TransportationChangeLog (TABLE)
PK: Id
Columns: Id int, LoadingNo varchar(20), DoNo varchar(20), TransferDate datetime2 null, TransferBy varchar(20) null, FromDriverId int null, ResendFlag int null, ResendDate datetime2 null, ResendBy int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: LoadingNo -> dbo.Delivery.LoadingNo; DoNo -> dbo.Delivery.DONo

### nss.SaleflowLog (TABLE)
PK: LogId
Columns: LogId bigint, Endpoint varchar(500), RequestPath nvarchar(2048), RequestHeader nvarchar(max) null, RequestBody nvarchar(max) null, RequestGrouping nvarchar(1024) null, ResponseStatusCode int null, ResponseHeader nvarchar(max) null, ResponseBody nvarchar(max) null, OrderCancellationId bigint null, EmailStatus bit null, EmailTo nvarchar(max) null, EmailCc nvarchar(max) null, Remark nvarchar(max) null, EmailRemark nvarchar(max) null, ErrorMessage nvarchar(max) null, RetryNumber int null, CreatedAt datetime2

### rc.BsProductionWeek (TABLE)
PK: ProductionDate
Columns: ProductionDate date, ProductionWeek varchar(20), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### rc.DeleteReceiveDetailLog (TABLE)
PK: ReceiveDetailId
Columns: ReceiveDetailId int, ReceiveId int null, SerialNo varchar(20) null, GtCode varchar(20) null, Grade varchar(20) null, ProductionDate datetime2 null, ProductionWeek varchar(20) null, ScanDate datetime2 null, ScanBy varchar(20) null, ReturnFlag int null, TcFlag int null, TsgFlag int null, DocumentNo varchar(20) null, SerialId int null, ReceiveType varchar(20) null, ReworkFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### rc.DeleteReceiveLog (TABLE)
PK: ReceiveId
Columns: ReceiveId int, Warehouse varchar(20), ReceiveType varchar(20) null, QrCode varchar(200) null, TagNo varchar(50) null, LoadingNo varchar(20) null, ReturnDocument varchar(20) null, ReceiveDate datetime2 null, ReceiveBy varchar(20) null, Qty int null, CutOffDate date null, EwmTag varchar(100) null, MappingCompleteFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null, DeleteDate datetime2 null, DeleteBy varchar(20) null

### rc.ProductionSerial (TABLE)
PK: SerialId
Columns: SerialId int, Warehouse varchar(20) null, HistId bigint null, SerialNo varchar(20) null, ReceiveLocalDate datetime2 null, GtCode varchar(20) null, Grade varchar(20) null, ResultCode varchar(20) null, TsgFlag int null, TsgDate datetime2 null, WhitelistFlag int null, WhitelistDate datetime2 null, CuringDate datetime2 null, DotSerial varchar(20) null, UsageFlag int null, ReworkFlag int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### rc.ReceiveErrorLog (TABLE)
PK: ReceiveLogId
Columns: ReceiveLogId int, Warehouse varchar(20) null, QrCode varchar(200) null, SerialNo varchar(20) null, GtCode varchar(20) null, Reason varchar(200) null, Description varchar(100) null, ReceiveDate datetime2 null, ReceiveBy varchar(20) null, LogType varchar(20) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### rc.ReceiveInvestigate (TABLE)
PK: LogId
Columns: LogId int, Warehouse varchar(20) null, GtCode varchar(20) null, SerialNo varchar(20) null, ProductionDate datetime2 null, ProductionWeek varchar(20) null, Grade varchar(20) null, ReceiveDate datetime2 null, ReceiveBy varchar(20) null

### rc.ReceiveItem (TABLE)
PK: ReceiveId
Columns: ReceiveId int, Warehouse varchar(20), ReceiveType varchar(20) null, QrCode varchar(200) null, TagNo varchar(50) null, LoadingNo varchar(20) null, ReturnDocument varchar(20) null, ReceiveDate datetime2, ReceiveBy varchar(20), Qty int, CutOffDate date, ReceiveSubType varchar(20) null, EwmTag varchar(100) null, MappingCompleteFlag int null, MappingCompleteDate datetime2 null, Address varchar(20) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### rc.ReceiveItemDetail (TABLE)
PK: ReceiveDetailId
Columns: ReceiveDetailId int, ReceiveId int, SerialNo varchar(20), GtCode varchar(20), Grade varchar(20), ProductionDate datetime2, ProductionWeek varchar(20), ScanDate datetime2, ScanBy varchar(20), ReturnFlag int null, TcFlag int null, TsgFlag int null, DocumentNo varchar(20) null, SerialId int null, ReceiveType varchar(20) null, ReworkFlag int null, ManualWeekFlag int null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)
FK: ReceiveId -> rc.ReceiveItem.ReceiveId

### rc.ReturnItem (TABLE)
PK: ReturnId
Columns: ReturnId int, Warehouse varchar(20), DocumentNo varchar(20), QrCode varchar(200) null, TagNo varchar(20) null, ReturnType varchar(20), ReturnReason varchar(20) null, ReturnStatus varchar(20) null, Remark varchar(255) null, ReturnDate datetime2, ReturnBy varchar(20), Qty int, ReceiveQty int null, LastReceiveDate datetime2 null, GtCodeSummary varchar(255) null, CutOffDate date, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### rc.ReturnItemDetail (TABLE)
PK: ReturnDetailId
Columns: ReturnDetailId int, ReturnId int, SerialNo varchar(20), GtCode varchar(20), Grade varchar(20) null, ScanDate datetime2, ScanBy varchar(20), CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)
FK: ReturnId -> rc.ReturnItem.ReturnId

### rc.ReturnItemTag (TABLE)
PK: ReturnTagId
Columns: ReturnTagId int, ReturnId int, QrCode varchar(200), TagNo varchar(20), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: ReturnId -> rc.ReturnItem.ReturnId

### rc.TireCheckerFifoDot (TABLE)
PK: Warehouse, CustomerCode, GtCode
Columns: Warehouse varchar(20), CustomerCode varchar(20), GtCode varchar(20), ProductionDate datetime2 null, ProductionWeek varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### rc.WhitelistHistory (TABLE)
PK: WhitelistHistId
Columns: WhitelistHistId int, Warehouse varchar(20) null, SerialNo varchar(20) null, ReceiveLocalDate datetime2 null, GtCode varchar(20) null, Result varchar(20) null

### rc.WhitelistSerial (TABLE)
PK: WhitelistId
Columns: WhitelistId int, Warehouse varchar(20) null, SerialNo varchar(20), ReceiveLocalDate datetime2 null, GtCode varchar(20), ResultCode varchar(20) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### rc.WhitelistSize (TABLE)
PK: Warehouse, GtCode
Columns: Warehouse varchar(20), GtCode varchar(20), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### rc.zz_20221207_WhitelistSize (TABLE)
Columns: Warehouse varchar(20), GtCode varchar(20), CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### rcbatch.ProductionSerialNK (TABLE)
PK: SerialId, HistId
Columns: SerialId int, HistId bigint, Barcode varchar(20) null, ReceiveLocalDate datetime2 null, ProductionCode varchar(20) null, Grade varchar(20) null, TSGResult varchar(20) null, TSGFlag int null, TSGDate datetime2 null, WhiteListFlag int null, WhiteListDate datetime2 null, CuringDate datetime2 null, DotSerial varchar(20) null, CreateDate datetime2 null

### rcbatch.ProductionSerialRS (TABLE)
PK: SerialId, HistId
Columns: SerialId int, HistId bigint, Barcode varchar(20) null, ReceiveLocalDate datetime2 null, ProductionCode varchar(20) null, Grade varchar(20) null, TSGResult varchar(20) null, TSGFlag int null, TSGDate datetime2 null, WhiteListFlag int null, WhiteListDate datetime2 null, CuringDate datetime2 null, DotSerial varchar(20) null, CreateDate datetime2 null

### rcbatch.WhiteListCheckpoint (TABLE)
PK: ConfigId
Columns: ConfigId int, CheckpointCode varchar(50) null, Warehouse varchar(10) null, WhiteListCode varchar(50) null, WhiteListSizeCode varchar(50) null, TSGCode varchar(50) null, WhiteListHistCode datetime2 null

### rcbatch.WhiteListHistNK (TABLE)
PK: WhitelistHistId
Columns: WhitelistHistId int, Barcode varchar(20) null, Receive_local_date datetime2 null, Production_Code varchar(20) null, Result varchar(20) null, CreateDate datetime2 null

### rcbatch.WhiteListHistRS (TABLE)
PK: WhitelistHistId
Columns: WhitelistHistId int, Barcode varchar(20) null, Receive_local_date datetime2 null, Production_Code varchar(20) null, Result varchar(20) null, CreateDate datetime2 null

### rcbatch.WhiteListSizeNK (TABLE)
PK: WhiteListSizeId
Columns: WhiteListSizeId int, ProductionCode varchar(20) null, Createdate datetime2 null

### rcbatch.WhiteListSizeRS (TABLE)
PK: WhiteListSizeId
Columns: WhiteListSizeId int, ProductionCode varchar(20) null, Createdate datetime2 null

### report.BstlTransporterCostByTrip (TABLE)
PK: ReportId
Columns: ReportId int, PaymentPeriod datetime null, CreateDate datetime null, CreateBy varchar(20) null
FK: ReportId -> report.BstlTransporterCostByTrip.ReportId

### report.BstlTransporterCostByTripDetail (TABLE)
PK: DetailId
Columns: DetailId int, ReportId int, TransporterCode varchar(20), CreateDate datetime null, CreateBy varchar(20) null
FK: ReportId -> report.BstlTransporterCostByTrip.ReportId

### report.CompareCost (TABLE)
PK: ReportId
Columns: ReportId int, TripId bigint, CurrentCalculationType varchar(20) null, CurrentCostPerTrip decimal(18,2) null, CurrentCostPerTripTransporter decimal(18,2) null, CompareCalculationType varchar(20) null, CompareCostPerTrip decimal(18,2) null, CompareCostPerTripTransporter decimal(18,2) null, CreateDate datetime null, CreateBy varchar(20) null

### report.CompareCostDetail (TABLE)
PK: DetailId
Columns: DetailId int, ReportId int, TripDetailId bigint null, CurrentAmount decimal(18,2) null, CurrentAmountTransporter decimal(18,2) null, CompareAmount decimal(18,2) null, CompareAmountTransporter decimal(18,2) null, NoChargeRateFlag int null, CreateDate datetime null, CreateBy varchar(20) null
FK: ReportId -> report.CompareCost.ReportId

### report.CostPerPiece (TABLE)
PK: ReportId
Columns: ReportId int, TransactionDate datetime2 null, PeriodFrom datetime2 null, PeriodTo datetime2 null, CreateDate datetime null, CreateBy varchar(20) null

### report.CostPerPieceDetail (TABLE)
PK: DetailId
Columns: DetailId int, ReportId int null, DoNo varchar(20) null, GtCode varchar(20) null, PlanQty numeric(18,4) null, DeliveryQty numeric(18,4) null, Amount decimal(18,2) null, AmountTransporter decimal(18,2) null, CostPerPiece decimal(18,2) null, CostPerPieceTransporter decimal(18,2) null, CreateDate datetime null, CreateBy varchar(20) null
FK: ReportId -> report.CostPerPiece.ReportId

### report.CostPerPieceExport (TABLE)
PK: ReportId
Columns: ReportId int, TransactionDate datetime2 null, PeriodFrom datetime2 null, PeriodTo datetime2 null, CreateDate datetime null, CreateBy varchar(20) null

### report.CostPerPieceExportDetail (TABLE)
PK: DetailId
Columns: DetailId int, ReportId int null, InvoiceNo varchar(20) null, DoNo varchar(20) null, GtCode varchar(20) null, PlanQty numeric(18,4) null, Amount decimal(18,2) null, CostPerPiece decimal(18,2) null, CreateDate datetime null, CreateBy varchar(20) null
FK: ReportId -> report.CostPerPieceExport.ReportId

### report.CostPerTrip (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20) null, CreatedAt varchar(20) null

### report.CostPerTrip_FinalDC (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20) null, CreatedAt varchar(20) null

### report.CostPerTripDetail (TABLE)
PK: Id
Columns: Id int, ReportId int, Activity varchar(20) null, TripNoCd bigint null, TransportationName varchar(200) null, Departure_trip varchar(200) null, Destination_trip varchar(200) null, VehicleDescription varchar(100) null, TruckId varchar(100) null, DocHDNo varchar(100) null, DONo varchar(100) null, DocDetailNo varchar(100) null, Departure_detail varchar(200) null, Destination_detail varchar(200) null, DocType varchar(100) null, BrandName varchar(100) null, BrandGroup varchar(100) null, CustomerCode varchar(20) null, CustomerName varchar(200) null, ProductTypeCd varchar(20) null, ProductCd varchar(20) null, StBt varchar(20) null, Warehouse varchar(20) null, ProductDescription varchar(50) null, BSTLProductSizeCd varchar(20) null, TBSCLProductSizeCd varchar(20) null, Quantity int null, ProductTBSCLCost decimal(18,2) null, ProductBSTLCost decimal(18,2) null, DiscountRate decimal(5,2) null, DiscountCost decimal(18,2) null, IsImport varchar(20) null, IsCassing varchar(1) null, IsTransfer varchar(1) null, Inactive varchar(1) null, Market varchar(20) null, DoDueDate datetime2 null, DeliveryDate datetime2 null, CustomerReceiptDate datetime2 null, OriginalType varchar(20) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, QTY int null, UnitPrice decimal(18,2) null, Price decimal(18,2) null, UnitPriceBSTL decimal(18,2) null, PriceBSTL decimal(18,2) null, TripNoEsimateCost bigint null, LoadingNo varchar(20) null, OriginalAmount decimal(18,2) null, OriginalAmountBSTL decimal(18,2) null
FK: ReportId -> report.CostPerTrip.ReportId

### report.CostPerTripDetail_FinalDC (TABLE)
PK: Id
Columns: Id int, ReportId int, Activity varchar(20) null, TripNoCd bigint null, TransportationName varchar(200) null, Departure_trip varchar(200) null, Destination_trip varchar(200) null, VehicleDescription varchar(100) null, TruckId varchar(100) null, DocHDNo varchar(100) null, DONo varchar(100) null, DocDetailNo varchar(100) null, Departure_detail varchar(200) null, Destination_detail varchar(200) null, DocType varchar(100) null, BrandName varchar(100) null, BrandGroup varchar(100) null, CustomerCode varchar(20) null, CustomerName varchar(200) null, ProductTypeCd varchar(20) null, ProductCd varchar(20) null, StBt varchar(20) null, Warehouse varchar(20) null, ProductDescription varchar(50) null, BSTLProductSizeCd varchar(20) null, TBSCLProductSizeCd varchar(20) null, Quantity int null, ProductTBSCLCost decimal(18,2) null, ProductBSTLCost decimal(18,2) null, DiscountRate decimal(5,2) null, DiscountCost decimal(18,2) null, IsImport varchar(20) null, IsCassing varchar(1) null, IsTransfer varchar(1) null, Inactive varchar(1) null, Market varchar(20) null, DoDueDate datetime2 null, DeliveryDate datetime2 null, CustomerReceiptDate datetime2 null, OriginalType varchar(20) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, QTY int null, UnitPrice decimal(18,2) null, Price decimal(18,2) null, UnitPriceBSTL decimal(18,2) null, PriceBSTL decimal(18,2) null, TripNoEsimateCost bigint null, LoadingNo varchar(20) null, BFAmount decimal(18,2) null, BFAmountTransporter decimal(18,2) null
FK: ReportId -> report.CostPerTrip_FinalDC.ReportId

### report.CostPerUnit (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20) null, CreatedAt varchar(20) null

### report.CostPerUnit_FinalDC (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20) null, CreatedAt varchar(20) null

### report.CostPerUnitDetail (TABLE)
PK: Id
Columns: Id int, ReportId int, Activity varchar(100) null, TripNoCd bigint null, TransportationName varchar(200) null, Departure varchar(200) null, Destination varchar(200) null, VehicleDescription varchar(100) null, TruckId varchar(100) null, LoadingNo varchar(20) null, DocHDNo varchar(100) null, DocDetailNo varchar(100) null, DocType varchar(100) null, BrandName varchar(100) null, BrandGroup varchar(100) null, CustomerCode varchar(20) null, CustomerName varchar(200) null, ProductTypeCd varchar(20) null, ProductCd varchar(20) null, WarehouseCode varchar(20) null, WarehouseName varchar(100) null, StBt varchar(20) null, ProductDescription varchar(50) null, BSTLProductSizeCd varchar(20) null, TBSCLProductSizeCd varchar(20) null, Quantity int null, ProductTBSCLCost decimal(18,2) null, ProductBSTLCost decimal(18,2) null, TotalProductTBSCLCost decimal(18,2) null, TotalProductBSTLCost decimal(18,2) null, DiscountRate decimal(5,2) null, DiscountCost decimal(18,2) null, IsImport varchar(20) null, IsCassing varchar(1) null, IsTransfer varchar(1) null, Inactive varchar(1) null, Market varchar(20) null, DoDueDate datetime2 null, DeliveryDate datetime2 null, CustomerReceiptDate datetime2 null, OriginalType varchar(20) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, DONo varchar(100) null, OriginalAmountTBSCL decimal(18,2) null, OriginalAmountBSTL decimal(18,2) null
FK: ReportId -> report.CostPerUnit.ReportId

### report.CostPerUnitDetail_FinalDC (TABLE)
PK: Id
Columns: Id int, ReportId int, Activity varchar(100) null, TripNoCd bigint null, TransportationName varchar(200) null, Departure varchar(200) null, Destination varchar(200) null, VehicleDescription varchar(100) null, TruckId varchar(100) null, LoadingNo varchar(20) null, DocHDNo varchar(100) null, DocDetailNo varchar(100) null, DocType varchar(100) null, BrandName varchar(100) null, BrandGroup varchar(100) null, CustomerCode varchar(20) null, CustomerName varchar(200) null, ProductTypeCd varchar(20) null, ProductCd varchar(20) null, WarehouseCode varchar(20) null, WarehouseName varchar(100) null, StBt varchar(20) null, ProductDescription varchar(50) null, BSTLProductSizeCd varchar(20) null, TBSCLProductSizeCd varchar(20) null, Quantity int null, ProductTBSCLCost decimal(18,2) null, ProductBSTLCost decimal(18,2) null, TotalProductTBSCLCost decimal(18,2) null, TotalProductBSTLCost decimal(18,2) null, DiscountRate decimal(5,2) null, DiscountCost decimal(18,2) null, IsImport varchar(20) null, IsCassing varchar(1) null, IsTransfer varchar(1) null, Inactive varchar(1) null, Market varchar(20) null, DoDueDate datetime2 null, DeliveryDate datetime2 null, CustomerReceiptDate datetime2 null, OriginalType varchar(20) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, DONo varchar(100) null, BFAmount decimal(18,2) null, BFAmountTransporter decimal(18,2) null
FK: ReportId -> report.CostPerUnit_FinalDC.ReportId

### report.OeTransfer (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20) null, CreatedAt varchar(20) null

### report.OeTransferCustomer (TABLE)
PK: Id
Columns: Id int, ReportId int, TransportationCompany varchar(200) null, PaymentRS decimal(18,2) null, TransportRS decimal(18,2) null, TbsclRS decimal(18,2) null, PaymentNK decimal(18,2) null, TransportNK decimal(18,2) null, TbsclNK decimal(18,2) null, PaymentCH decimal(18,2) null, TransportCH decimal(18,2) null, TbsclCH decimal(18,2) null, PaymentCHOE decimal(18,2) null, TransportCHOE decimal(18,2) null, TbsclCHOE decimal(18,2) null, PaymentTotal decimal(18,2) null, TransportTotal decimal(18,2) null, TbsclTotal decimal(18,2) null, SequenceNo int null
FK: ReportId -> report.OeTransfer.ReportId

### report.OeTransferSummary (TABLE)
PK: Id
Columns: Id int, ReportId int, TransportationCompany varchar(200) null, PaymentTotal decimal(18,2) null, TransportTotal decimal(18,2) null, TbsclTotal decimal(18,2) null, SequenceNo int null
FK: ReportId -> report.OeTransfer.ReportId

### report.OeTransferWarehouse (TABLE)
PK: Id
Columns: Id int, ReportId int, TransportationCompany varchar(200) null, Payment1 decimal(18,2) null, Transport1 decimal(18,2) null, Tbscl1 decimal(18,2) null, Payment2 decimal(18,2) null, Transport2 decimal(18,2) null, Tbscl2 decimal(18,2) null, Payment3 decimal(18,2) null, Transport3 decimal(18,2) null, Tbscl3 decimal(18,2) null, Payment4 decimal(18,2) null, Transport4 decimal(18,2) null, Tbscl4 decimal(18,2) null, Payment5 decimal(18,2) null, Transport5 decimal(18,2) null, Tbscl5 decimal(18,2) null, PaymentTotal decimal(18,2) null, TransportTotal decimal(18,2) null, TbsclTotal decimal(18,2) null, SequenceNo int null
FK: ReportId -> report.OeTransfer.ReportId

### report.ReceivingTsgSummary (TABLE)
PK: ReportId
Columns: ReportId int, Warehouse varchar(20) null, Month int null, Year int null, Title int null, Day1 int null, Day2 int null, Day3 int null, Day4 int null, Day5 int null, Day6 int null, Day7 int null, Day8 int null, Day9 int null, Day10 int null, Day11 int null, Day12 int null, Day13 int null, Day14 int null, Day15 int null, Day16 int null, Day17 int null, Day18 int null, Day19 int null, Day20 int null, Day21 int null, Day22 int null, Day23 int null, Day24 int null, Day25 int null, Day26 int null, Day27 int null, Day28 int null, Day29 int null, Day30 int null, Day31 int null, AsOfDate datetime2 null, ProcessDate datetime2 null, ProcessBy varchar(20) null

### report.StickerStockSummary (TABLE)
PK: StickerRptId
Columns: StickerRptId int, Warehouse varchar(20) null, LabelStockDate datetime2 null, StockTime datetime2 null, Market varchar(20) null, MovementCode varchar(20) null, StickerCode varchar(50) null, StickerBarcode varchar(50) null, LabelSizeName varchar(50) null, GTNote varchar(1000) null, StockQty bigint null, StockTurnOver decimal(11,2) null, MTDAccuPlan bigint null, MTDAccuActual bigint null, YTDAccuPlan bigint null, YTDAccuActual bigint null, PYYear bigint null, Jan bigint null, Feb bigint null, Mar bigint null, Apr bigint null, May bigint null, Jun bigint null, Jul bigint null, Aug bigint null, Sep bigint null, Oct bigint null, Nov bigint null, Dec bigint null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### report.Tbsc (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20), CreatedAt varchar(20)

### report.Tbsc_FinalDC (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20), CreatedAt varchar(20)

### report.TbscDetail (TABLE)
PK: Id
Columns: Id int, ReportId int, Code varchar(200) null, ReportAmountGroup varchar(200) null, ItemOrder int null, TransportationCompany varchar(200) null, BSDomestic decimal(18,2) null, BSImport decimal(18,2) null, BSAlloy decimal(18,2) null, BSCassing decimal(18,2) null, FSDomestic decimal(18,2) null, FSImport decimal(18,2) null, DT decimal(18,2) null, Scrap decimal(18,2) null, Total decimal(18,2) null
FK: ReportId -> report.Tbsc.ReportId

### report.TbscDetail_FinalDC (TABLE)
PK: Id
Columns: Id int, ReportId int, Code varchar(200) null, ReportAmountGroup varchar(200) null, ItemOrder int null, TransportationCompany varchar(200) null, BSDomestic decimal(18,2) null, BSImport decimal(18,2) null, BSAlloy decimal(18,2) null, BSCassing decimal(18,2) null, FSDomestic decimal(18,2) null, FSImport decimal(18,2) null, DT decimal(18,2) null, Scrap decimal(18,2) null, Total decimal(18,2) null, BFAmount decimal(18,2) null, BFAmountTransporter decimal(18,2) null
FK: ReportId -> report.Tbsc_FinalDC.ReportId

### report.TireStockAsOnSummary (TABLE)
PK: ReportId
Columns: ReportId int, Month int null, Year int null, Note varchar(255) null, CreateDate datetime2 null, CreateBy varchar(20) null

### report.TireStockAsOnSummaryDetail (TABLE)
PK: DetailId
Columns: DetailId int, ReportId int null, Warehouse varchar(20) null, GtCode varchar(20) null, Grade varchar(20) null, ProductionReceive int null, TireCheckerLoaded int null, TireTotal int null, RackTotal int null, AsOfDate datetime2 null, ProcessDate datetime2 null, ProcessBy varchar(20) null

### report.TransporterCost (TABLE)
PK: ReportId
Columns: ReportId int, CreatedBy varchar(20) null, CreatedAt varchar(20) null

### report.TransporterCostDetail (TABLE)
PK: Id
Columns: Id int, ReportId int, ReportType varchar(200) null, BrandGroup varchar(100) null, Transporter varchar(200) null, PaymentPeriod datetime2 null, Departure varchar(200) null, Destination varchar(200) null, TireCategorySeqNo int null, TireCategory varchar(100) null, GroupT int null, GroupC int null, GroupF int null, GroupR int null, GroupA int null, GroupOT int null, UnitPriceT decimal(18,2) null, UnitPriceC decimal(18,2) null, UnitPriceF decimal(18,2) null, UnitPriceR decimal(18,2) null, UnitPriceA decimal(18,2) null, UnitPriceOT decimal(18,2) null
FK: ReportId -> report.TransporterCost.ReportId

### routing.ConvertRouting (TABLE)
PK: ProcessId
Columns: ProcessId int, ProcessDate datetime2 null, ProcessBy varchar(20) null

### routing.ConvertRoutingDetail (TABLE)
PK: DetailId
Columns: DetailId int, ProcessId int null, RoutingNo varchar(20) null, LoadingNo varchar(20) null, PlanDate datetime2 null, PickingStartTime datetime2 null, PickingFinishTime datetime2 null, TruckArrivedTime datetime2 null, LoadingStartTime datetime2 null, LoadingFinishTime datetime2 null, DeliveryTime datetime2 null, TimespanForTruckArrived int null, TimespanForLoading int null, TransportCompany varchar(50) null, TruckSize varchar(20) null, LicensePlate varchar(100) null, DoNo varchar(20) null, TransferNo varchar(20) null, PlanDeliveryDate datetime2 null, CustomerCode varchar(20) null, InvoiceNo varchar(20) null, Market varchar(20) null, Warehouse varchar(20) null, GtCode varchar(20) null, SaleCode varchar(20) null, LoadQty numeric(18,4) null, BatchNo varchar(20) null, Grade varchar(20) null, ProductionDate datetime2 null, SkipFifoDotFlag int null, ProcessDate datetime2 null, ProcessBy varchar(20) null
FK: ProcessId -> routing.ConvertRouting.ProcessId

### routing.DOPendingCart (TABLE)
PK: ID
Columns: ID int, UserID int, DONo varchar(20), TransferNo varchar(20), Username varchar(20), CreateBy varchar(20), CreateDate datetime2

### routing.PreCalculation (TABLE)
PK: PreCalculationId
Columns: PreCalculationId int, RoutingNo varchar(20) null, Warehouse varchar(20) null, Market varchar(20) null, Quantity decimal(15,6) null, ZoneId int null, DeliveryDate datetime2 null, CalculationType varchar(20) null, MainTransporter varchar(50) null, DistributorFlag int null, TruckType varchar(20) null, TripDestination int null, TransferTransporter varchar(50) null, TransferTruckType varchar(20) null, CostPerTrip decimal(18,2) null, CostPerTripTransporter decimal(18,2) null, PremiumFreight decimal(18,2) null, PremiumFreightTransporter decimal(18,2) null, ConfirmFlag int null, ConfirmRemark nvarchar(1000) null, ConfirmDate datetime2 null, ConfirmBy varchar(20) null, CancelFlag int null, CancelRemark nvarchar(1000) null, CancelDate datetime2 null, CancelBy varchar(20) null, RunningNo int null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### routing.PreCalculationDetail (TABLE)
PK: ResultId
Columns: ResultId int, PreCalculationId int null, Transporter varchar(50) null, TruckType varchar(20) null, CalculationType varchar(20) null, Departure int null, Destination int null, EstimateCost decimal(18,2) null, EstimateCostTransporter decimal(18,2) null, PremiumFreight decimal(18,2) null, PremiumFreightTransporter decimal(18,2) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: PreCalculationId -> routing.PreCalculation.PreCalculationId

### routing.PreCalculationEstimateCost (TABLE)
PK: EstimateCostId
Columns: EstimateCostId int, CalculateId int, RoutingNo varchar(20) null, Warehouse varchar(20) null, DeliveryDate datetime2 null, Market varchar(20) null, CalculationType varchar(20) null, Transporter varchar(20) null, TruckType varchar(20) null, Departure int null, Destination int null, EstimateCost decimal(18,2) null, EstimateCostTransporter decimal(18,2) null, PremiumFreight decimal(18,2) null, PremiumFreightTransporter decimal(18,2) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null
FK: CalculateId -> routing.PreCalculationEstimateCostCriteria.CalculateId

### routing.PreCalculationEstimateCostCriteria (TABLE)
PK: CalculateId
Columns: CalculateId int, RoutingNo varchar(20) null, DeliveryDate datetime2 null, CalculationType varchar(20) null, MainTransporter varchar(50) null, DistributorFlag int null, TruckType varchar(20) null, TripDestination int null, TransferTransporter varchar(50) null, TransferTruckType varchar(20) null, BatchNo varchar(50) null, CreateDate datetime2 null, CreateBy varchar(20) null, UpdateDate datetime2 null, UpdateBy varchar(20) null

### routing.PreCalculationEstimateCostDetail (TABLE)
PK: DetailId
Columns: DetailId int, EstimateCostId int, DoNo varchar(20) null, TransferNo varchar(20) null, CustomerCode varchar(20) null, Departure int null, Destination int null, GtType varchar(20) null, GtCode varchar(20) null, TireSize varchar(20) null, Quantity decimal(18,2) null, CostPerUnit decimal(18,2) null, CostPerUnitTransporter decimal(18,2) null, EstimateCost decimal(18,2) null, EstimateCostTransporter decimal(18,2) null, PremiumFreight decimal(18,2) null, PremiumFreightTransporter decimal(18,2) null, CreateDate datetime2 null, CreateBy nchar(10) null, UpdateDate datetime2 null, UpdateBy nchar(10) null
FK: EstimateCostId -> routing.PreCalculationEstimateCost.EstimateCostId

### routing.RouteDO (TABLE)
PK: RoutingNo, DONo, TransferNo
Columns: RoutingNo varchar(20), DONo varchar(20), TransferNo varchar(20), SeqNo int, CustomerCode varchar(20), CustomerName varchar(150), ShipTo varchar(255), WarehouseCode varchar(20), Noted varchar(20) null, GenerateNote varchar(255) null, GenerateDate datetime2 null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### routing.RouteDODetail (TABLE)
PK: RoutingNo, DONo, TransferNo, GTCode
Columns: RoutingNo varchar(20), DONo varchar(20), TransferNo varchar(20), GTCode varchar(20), SpecCode varchar(10), SizeName varchar(50), TranQty decimal(15,6), LoadedQty decimal(15,6), RouteType char(1) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20)

### routing.RouteParameterLog (TABLE)
PK: RunNo
Columns: RunNo int, GenerateBatchNo varchar(20) null, CallMethod varchar(100) null, ParameterDescription varchar(2000) null, CreateBy varchar(20) null, CreateDate datetime null, FromHost varchar(200) null

### routing.RouteTruck (TABLE)
PK: RoutingNo
Columns: RoutingNo varchar(20), Capacity decimal(15,6), CapacityPercent decimal(15,6), LoadingDate datetime2, TransportationCode varchar(50), ZoneID int null, TransportationPlateID int null, PlateNo varchar(10) null, PlateProvinceCode varchar(10) null, TruckCapacity decimal(15,6), WarehouseCode varchar(20) null, MarketCode varchar(20) null, ParentRoutingNo varchar(20) null, RoutingStatus varchar(20), GenerateBatchNo varchar(20) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2, UpdateBy varchar(20), ApproveDate datetime2 null, ApproveBy varchar(20) null, PrintCount int null, PrintDate datetime2 null, PrintBy varchar(20) null, WhNo varchar(20) null, Plant varchar(20) null, Sloc varchar(20) null, ConfirmToSapFlag int null, TransferDate datetime2 null, ConvertTruckFlag int null

### sap.DeliveryOrderImport (TABLE)
PK: Id
Columns: Id int, TransactionNo varchar(50), ReadLineNo int, ReadAt datetime, DeliveryOrderNo varchar(200) null, ShippingPoint varchar(200) null, ShippingCondition varchar(200) null, DeliveryPriority varchar(200) null, DeliveryDate varchar(200) null, DeliveryTime varchar(200) null, ShipToCode varchar(200) null, ShipToName1 varchar(200) null, ShipToName2 varchar(200) null, ShipToName3 varchar(200) null, ShipToName4 varchar(200) null, ShipToStreet1 varchar(200) null, ShipToStreet2 varchar(200) null, ShipToStreet3 varchar(200) null, ShipToStreet4 varchar(200) null, ShipToStreet5 varchar(200) null, ShipToDistrict varchar(200) null, RegionCode varchar(200) null, RegionDescription varchar(200) null, ShipToPostCode varchar(200) null, ShipToEmail varchar(200) null, ShipToTelephone varchar(200) null, ShipToMobile varchar(200) null, ExportInvoiceNo varchar(200) null, DeliveryRemark varchar(200) null, AcceptanceTireAge varchar(200) null, CreatedBy varchar(200) null, CreatedTime varchar(200) null, CreatedOn varchar(200) null, ChangedBy varchar(200) null, ChangedOn varchar(200) null, DeletionFlag varchar(200) null, DeliveryMethod varchar(200) null

### sap.DeliveryOrderImportDetail (TABLE)
PK: Id
Columns: Id int, DeliveryOrderImportId int, SequenceNo int, ReadLineNo int, ReadAt datetime, DeliveryOrderNo varchar(200) null, DeliveryItem varchar(200) null, MaterialNumber varchar(200) null, MaterialDescription varchar(200) null, MaterialGtCode varchar(200) null, MaterialGrade varchar(200) null, MaterialTireCategory varchar(200) null, MaterialDomesticType varchar(200) null, MaterialPhLevel2 varchar(200) null, MaterialPhLevel6 varchar(200) null, ActualQuantityDelivered varchar(200) null, SalesUnit varchar(200) null, Volume varchar(200) null, NetWeight varchar(200) null, BatchNumber varchar(200) null, BatchLastGrDate varchar(200) null, DeliveryItemCategory varchar(200) null, Plant varchar(200) null, StorageLocation varchar(200) null, EwmWhNo varchar(200) null, ExportDocNo varchar(200) null, ExportDoLine varchar(200) null
FK: DeliveryOrderImportId -> sap.DeliveryOrderImport.Id

### sap.ImportDOUnsuccessLog (TABLE)
PK: Id
Columns: Id int, ImportId int null, ImportDetailId int null, DoNo varchar(200) null, TransactionNo varchar(50) null, StoredFilePath varchar(200) null, NoticeFlag int null, Reason varchar(200) null, SapImportAt datetime2 null, CreateBy varchar(50) null, CreateDate datetime2 null, UpdateBy varchar(50) null, UpdateDate datetime2 null
FK: ImportId -> sap.DeliveryOrderImport.Id; ImportDetailId -> sap.DeliveryOrderImportDetail.Id

### sap.InterfaceHULog (TABLE)
PK: Id
Columns: Id int, EwmTag varchar(20) null, BpcPlus varchar(20) null, GtCode varchar(20) null, Qty int null, Week varchar(20) null, Address varchar(20) null, Grade varchar(20) null, ReceiveDate varchar(20) null, Shift varchar(20) null, Receiver varchar(40) null, SizeName varchar(40) null, InterfaceAt datetime2 null, InterfaceBy varchar(20) null

### sap.TraceLog (TABLE)
PK: Id
Columns: Id bigint, RequestAt datetime2, TraceIdentifier nvarchar(50), ApiCode nvarchar(100) null, DONo varchar(20) null, LoadingNo varchar(20) null, RequestUrl nvarchar(200), RequestHeader nvarchar(max), RequestBody nvarchar(max), ResponseHeader nvarchar(max) null, ResponseBody nvarchar(max) null, ResponseCode int, RetryCount int

### sap.vwMapSapImportToDeliveryOrder (VIEW)
Columns: Id int, DetailId int, DoNo varchar(20) null, WarehouseCode varchar(20) null, Market varchar(20) null, Noted varchar(20) null, DeliveryDate datetime2 null, CustomerCode varchar(20) null, CustomerName varchar(150) null, ShipTo varchar(255) null, ProvinceCode varchar(20) null, CreateBy varchar(20) null, CreateDate datetime2 null, UpdateBy varchar(20) null, UpdateDate datetime2 null, TransferNo varchar(20) null, BF varchar(50) null, DOStatus varchar(20) null, Remark varchar(200) null, GenerateNote varchar(255) null, GenerateDate datetime2 null, DeliveryItem varchar(200) null, SaleCode varchar(20) null, SizeName varchar(50) null, GTCode varchar(20) null, TranQty numeric(18,4) null, SapPlant varchar(50) null, SapStorageLocation varchar(50) null, SapWarehouse varchar(20) null, SpecCode varchar(10) null, FileCreatedDate datetime2 null, PartialFlag bit null, ReadAt datetime, TransactionNo varchar(50), StoredFilePath varchar(500), DeliveryItemCategory varchar(200) null, DeliveryMethod varchar(50) null, CreatedDate_DateTime datetime2 null, SapDeliveryDate datetime2 null

### tis.SyncTisData (TABLE)
PK: LastSyncDate
Columns: LastSyncDate datetime2

### weightchecker.WeightPlan (TABLE)
PK: PlanNo
Columns: PlanNo varchar(20), Warehouse varchar(20), DefaultStandardWeight numeric(15,6), DefaultMinimumWeight numeric(15,6), DefaultMaximumWeight numeric(15,6), ClientMinimumWeight numeric(15,6) null, ClientMaximumWeight numeric(15,6) null, CreateDate datetime2, CreateBy varchar(20), UpdateDate datetime2 null, UpdateBy varchar(20) null

### weightchecker.WeightPlanLog (TABLE)
PK: LogID
Columns: LogID int, PlanNo varchar(20), StandardWeight numeric(15,6), MinimumWeight numeric(15,6), MaximumWeight numeric(15,6), Weight numeric(15,6), StatusFlag int, CreateDate datetime2, CreateBy varchar(20)
FK: PlanNo -> weightchecker.WeightPlan.PlanNo
