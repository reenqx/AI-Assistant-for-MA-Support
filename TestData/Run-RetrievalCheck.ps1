<#
.SYNOPSIS
  Checks, WITHOUT calling the AI, that each test question finds the mock cases it should.

.DESCRIPTION
  For every question in test-questions.json that lists expectedCaseIds, calls the API's fast search endpoint
  (POST /api/chat/search - keyword search only, no AI, no cost) and reports whether every expected case is among
  the cases returned. Start the API with the mock KB first (see README.md). Read-only.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File TestData\Run-RetrievalCheck.ps1 -ApiBase http://localhost:5299
#>
param([string]$ApiBase = 'http://localhost:5299')

$ErrorActionPreference = 'Stop'
$file = Join-Path $PSScriptRoot 'test-questions.json'
$spec = Get-Content $file -Raw -Encoding UTF8 | ConvertFrom-Json

$pass = 0; $fail = 0; $skipped = 0; $known = 0
foreach ($q in $spec.questions) {
    if (-not $q.expectedCaseIds -or $q.expectedCaseIds.Count -eq 0) { $skipped++; continue }
    # steps that need an earlier turn or several UI actions cannot be asked as a plain question
    if ($q.question.StartsWith('(') -or $q.question.Contains([string][char]0x2192)) { $skipped++; continue }

    $body = @{ question = $q.question; conversationId = $null } | ConvertTo-Json -Compress
    $resp = Invoke-RestMethod -Uri "$ApiBase/api/chat/search" -Method Post -ContentType 'application/json; charset=utf-8' `
        -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 60
    $found = @($resp.cases | ForEach-Object { [int]$_.id })
    $missing = @($q.expectedCaseIds | Where-Object { $found -notcontains [int]$_ })
    if ($missing.Count -eq 0) {
        $pass++
        Write-Host ("PASS  {0,-4} {1}" -f $q.id, $q.question)
    } elseif ($q.knownIssue) {
        $known++
        Write-Host ("KNOWN {0,-4} {1}" -f $q.id, $q.question) -ForegroundColor Yellow
        Write-Host ("      {0}" -f $q.knownIssue) -ForegroundColor Yellow
    } else {
        $fail++
        Write-Host ("FAIL  {0,-4} {1}" -f $q.id, $q.question) -ForegroundColor Red
        Write-Host ("      expected {0} | got {1} | missing {2}" -f ($q.expectedCaseIds -join ','), ($found -join ','), ($missing -join ',')) -ForegroundColor Red
    }
}
Write-Host ""
Write-Host ("Retrieval check: {0} passed, {1} failed, {2} known issue, {3} skipped (need AI or several steps)" -f $pass, $fail, $known, $skipped)
if ($fail -gt 0) { exit 1 }
