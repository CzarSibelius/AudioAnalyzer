<#
.SYNOPSIS
    Reads a VSTest TRX file and writes sorted per-test durations (CSV + Markdown).

.PARAMETER TrxPath
    Path to tests.trx (e.g. tests/AudioAnalyzer.Tests/TestResults/tests.trx).

.PARAMETER OutDir
    Directory for slow-tests.csv and slow-tests.md (default: same folder as TRX).
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $TrxPath,

    [string] $OutDir = ""
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $TrxPath)) {
    Write-Error "TRX not found: $TrxPath"
}

if ([string]::IsNullOrWhiteSpace($OutDir)) {
    $OutDir = Split-Path -Parent $TrxPath
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

[xml] $doc = Get-Content -LiteralPath $TrxPath -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$ns.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

$rows = @()
foreach ($node in $doc.SelectNodes("//t:UnitTestResult", $ns)) {
    $name = $node.testName
    $dur = $node.duration
    if ([string]::IsNullOrWhiteSpace($name) -or [string]::IsNullOrWhiteSpace($dur)) { continue }
    $ts = [TimeSpan]::Zero
    $fmt = "hh\:mm\:ss\.fffffff"
    $ok = [TimeSpan]::TryParseExact(
        $dur,
        $fmt,
        [System.Globalization.CultureInfo]::InvariantCulture,
        [ref]$ts)
    if (-not $ok) {
        [void][TimeSpan]::TryParse($dur, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$ts)
    }
    $rows += [PSCustomObject]@{ TestName = $name; Duration = $ts; DurationSeconds = $ts.TotalSeconds }
}

$ordered = $rows | Sort-Object DurationSeconds -Descending

$csvPath = Join-Path $OutDir "slow-tests.csv"
$mdPath = Join-Path $OutDir "slow-tests.md"

$ordered | Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8

$times = $doc.SelectSingleNode("//t:Times", $ns)
$start = if ($times) { $times.start } else { "" }
$finish = if ($times) { $times.finish } else { "" }

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Test duration report (from TRX)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| TRX | Value |")
[void]$sb.AppendLine("|-----|-------|")
[void]$sb.AppendLine("| File | ``$TrxPath`` |")
[void]$sb.AppendLine("| Times start | $start |")
[void]$sb.AppendLine("| Times finish | $finish |")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Slowest tests (top 40)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Rank | Seconds | Test |")
[void]$sb.AppendLine("|------|---------|------|")

$rank = 1
foreach ($r in ($ordered | Select-Object -First 40)) {
    $sec = [math]::Round($r.DurationSeconds, 3)
    $safeName = $r.TestName -replace '\|', '\|'
    [void]$sb.AppendLine("| $rank | $sec | ``$safeName`` |")
    $rank++
}

[void]$sb.AppendLine("")
$sumSec = ($rows | Measure-Object -Property DurationSeconds -Sum).Sum
[void]$sb.AppendLine("**Sum of per-test durations:** $([math]::Round($sumSec, 1)) s (includes parallel overlap; wall clock is in Times).")
[void]$sb.AppendLine("")

[System.IO.File]::WriteAllText($mdPath, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $csvPath"
Write-Host "Wrote $mdPath"
