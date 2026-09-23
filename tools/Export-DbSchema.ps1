<#
.SYNOPSIS
  Regenerates MoCS-Schema-Full.md (every table/view: columns, PK, FK) from the live database.

.DESCRIPTION
  READ-ONLY. Only queries INFORMATION_SCHEMA and the sys catalog views (metadata) - it never
  reads table data and never writes to the database. The output is what the API's
  FileDbSchemaProvider picks tables from when it builds a prompt, so re-run this whenever the
  database structure changes, then restart the API.

  The connection string is taken from the first of these that exists (the same string the API
  itself uses; it is never printed or written to the output file):
    1. -ConnectionString
    2. environment variable MOCS_CONNECTION_STRING
    3. dotnet user-secrets   (ConnectionStrings:MoCS of the Api project)
    4. src/AIforMAsupport.Api/appsettings.Development.json
    5. src/AIforMAsupport.Api/appsettings.json

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\Export-DbSchema.ps1

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\Export-DbSchema.ps1 -OutputPath C:\temp\schema-check.md
#>
[CmdletBinding()]
param(
    [string]$ConnectionString,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$apiDir = Join-Path $projectRoot 'src\AIforMAsupport.Api'
if (-not $OutputPath) { $OutputPath = Join-Path $projectRoot 'data\MoCS-Schema-Full.md' }

function Get-ConfiguredConnectionString {
    if ($env:MOCS_CONNECTION_STRING) { return $env:MOCS_CONNECTION_STRING }

    try {
        $secrets = & dotnet user-secrets list --project $apiDir 2>$null
        foreach ($line in $secrets) {
            if ($line -like 'ConnectionStrings:MoCS = *') { return $line.Substring('ConnectionStrings:MoCS = '.Length) }
        }
    } catch { }

    foreach ($file in 'appsettings.Development.json', 'appsettings.json') {
        $path = Join-Path $apiDir $file
        if (Test-Path $path) {
            $cfg = Get-Content $path -Raw | ConvertFrom-Json
            if ($cfg.ConnectionStrings -and $cfg.ConnectionStrings.MoCS) { return $cfg.ConnectionStrings.MoCS }
        }
    }
    return $null
}

if (-not $ConnectionString) { $ConnectionString = Get-ConfiguredConnectionString }
if (-not $ConnectionString) { throw 'No connection string found. Pass -ConnectionString or set ConnectionStrings:MoCS for the Api project.' }

# System.Data.SqlClient ships with Windows PowerShell 5.1 only, so PowerShell 7 hands the run over to it.
if ($PSVersionTable.PSEdition -eq 'Core') {
    $forward = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $PSCommandPath, '-OutputPath', $OutputPath, '-ConnectionString', $ConnectionString)
    & powershell.exe @forward
    exit $LASTEXITCODE
}
Add-Type -AssemblyName System.Data
$connectionType = 'System.Data.SqlClient.SqlConnection'

$conn = New-Object $connectionType $ConnectionString
$conn.Open()
$database = $conn.Database
Write-Host "Connected to database '$database' (read-only metadata queries)."

function Invoke-MetadataQuery([string]$Sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $Sql
    $cmd.CommandTimeout = 60
    $reader = $cmd.ExecuteReader()
    $rows = New-Object System.Collections.Generic.List[object]
    while ($reader.Read()) {
        $row = [ordered]@{}
        for ($i = 0; $i -lt $reader.FieldCount; $i++) {
            $value = $reader.GetValue($i)
            if ($value -is [System.DBNull]) { $value = $null }
            $row[$reader.GetName($i)] = $value
        }
        $rows.Add([pscustomobject]$row)
    }
    $reader.Close()
    return $rows
}

try {
    $columns = Invoke-MetadataQuery @"
SELECT c.TABLE_SCHEMA AS [schema], c.TABLE_NAME AS [table], t.TABLE_TYPE AS [type],
       c.COLUMN_NAME AS [column], c.ORDINAL_POSITION AS [pos], c.DATA_TYPE AS [dataType],
       c.CHARACTER_MAXIMUM_LENGTH AS [maxLen], c.NUMERIC_PRECISION AS [prec], c.NUMERIC_SCALE AS [scale],
       c.IS_NULLABLE AS [nullable]
FROM INFORMATION_SCHEMA.COLUMNS c
JOIN INFORMATION_SCHEMA.TABLES t ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
"@

    $primaryKeys = Invoke-MetadataQuery @"
SELECT kcu.TABLE_SCHEMA AS [schema], kcu.TABLE_NAME AS [table], kcu.COLUMN_NAME AS [column], kcu.ORDINAL_POSITION AS [pos]
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
  ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA AND kcu.TABLE_NAME = tc.TABLE_NAME
WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
ORDER BY kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.ORDINAL_POSITION
"@

    $foreignKeys = Invoke-MetadataQuery @"
SELECT OBJECT_SCHEMA_NAME(fk.parent_object_id) AS [schema], OBJECT_NAME(fk.parent_object_id) AS [table],
       COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS [column],
       OBJECT_SCHEMA_NAME(fk.referenced_object_id) AS [refSchema], OBJECT_NAME(fk.referenced_object_id) AS [refTable],
       COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS [refColumn]
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
ORDER BY 1, 2, fkc.constraint_column_id
"@
}
finally {
    $conn.Close()
}

function Get-TypeText($c) {
    $t = $c.dataType
    if ($t -in 'varchar', 'nvarchar', 'char', 'nchar', 'varbinary', 'binary') {
        $len = if ($null -eq $c.maxLen -or $c.maxLen -eq -1) { 'max' } else { $c.maxLen }
        return "$t($len)"
    }
    if ($t -in 'decimal', 'numeric') { return "$t($($c.prec),$($c.scale))" }
    return $t
}

# One entry per table/view, keyed "schema.table".
$tables = [ordered]@{}
foreach ($c in $columns) {
    $key = "$($c.schema).$($c.table)"
    if (-not $tables.Contains($key)) {
        $tables[$key] = [pscustomobject]@{
            Schema = $c.schema; Table = $c.table; Type = $c.type
            Columns = New-Object System.Collections.Generic.List[object]
            Pk = New-Object System.Collections.Generic.List[string]
            Fk = New-Object System.Collections.Generic.List[object]
        }
    }
    $tables[$key].Columns.Add($c)
}
foreach ($p in $primaryKeys) { $key = "$($p.schema).$($p.table)"; if ($tables.Contains($key)) { $tables[$key].Pk.Add($p.column) } }
foreach ($f in $foreignKeys) { $key = "$($f.schema).$($f.table)"; if ($tables.Contains($key)) { $tables[$key].Fk.Add($f) } }

# dbo first, then the other schemas alphabetically; tables alphabetically within a schema.
$ordered = $tables.Values | Sort-Object @{ Expression = { if ($_.Schema -eq 'dbo') { '0' + $_.Schema } else { '1' + $_.Schema } } }, Table

$out = New-Object System.Collections.Generic.List[string]
$today = Get-Date -Format 'yyyy-MM-dd'
$out.Add('# MoCS Database Schema (full, generated)')
$out.Add('')
$out.Add("สร้างอัตโนมัติจากเมทาดาทาของฐานข้อมูล ``$database`` (INFORMATION_SCHEMA + sys catalog) เมื่อ $today — ไม่มีข้อมูลในตารางปนอยู่ ไฟล์นี้ **ไม่ถูกส่งเข้า prompt ทั้งไฟล์** ระบบเลือกเฉพาะตารางที่เกี่ยวข้องกับคำถามให้ (ดู ``FileDbSchemaProvider``) ส่วนคำอธิบายความหมายของคอลัมน์และกติกาการอ้างชื่อ อยู่ใน ``MoCS-Schema-Reference.md``")
$out.Add('')
$out.Add('รูปแบบต่อ 1 ตาราง: หัวข้อ `### schema.ชื่อ (TABLE|VIEW)` ตามด้วย `PK:` (ถ้ามี), `Columns:` (คอลัมน์ที่ไม่ระบุ null = NOT NULL), `FK:` (ถ้ามี) — ถ้าตารางในฐานเปลี่ยน ให้สร้างไฟล์นี้ใหม่ด้วย `tools/Export-DbSchema.ps1`')
$out.Add('')

foreach ($t in $ordered) {
    $kind = if ($t.Type -eq 'VIEW') { 'VIEW' } else { 'TABLE' }
    $out.Add("### $($t.Schema).$($t.Table) ($kind)")
    if ($t.Pk.Count -gt 0) { $out.Add('PK: ' + ($t.Pk -join ', ')) }
    $cols = foreach ($c in $t.Columns) { "$($c.column) $(Get-TypeText $c)" + $(if ($c.nullable -eq 'YES') { ' null' } else { '' }) }
    $out.Add('Columns: ' + ($cols -join ', '))
    if ($t.Fk.Count -gt 0) {
        $fks = foreach ($f in $t.Fk) { "$($f.column) -> $($f.refSchema).$($f.refTable).$($f.refColumn)" }
        $out.Add('FK: ' + ($fks -join '; '))
    }
    $out.Add('')
}

[System.IO.File]::WriteAllText($OutputPath, ($out -join "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
Write-Host ("Wrote {0} tables/views ({1} columns) to {2}" -f $tables.Count, $columns.Count, $OutputPath)
