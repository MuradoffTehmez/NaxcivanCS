param(
    [string]$ResultsDirectory = 'TestResults',
    [ValidateRange(0, 100)][double]$Threshold = 70
)

$ErrorActionPreference = 'Stop'
$reports = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter coverage.cobertura.xml -Recurse)
if (-not $reports.Count) { throw 'No Cobertura reports found.' }
$groups = @{ Shared = @{}; Backend = @{}; Server = @{} }
foreach ($report in $reports) {
    [xml]$xml = Get-Content -LiteralPath $report.FullName -Raw
    $source = @($xml.SelectNodes('/coverage/sources/source') | ForEach-Object { $_.InnerText } | Where-Object { $_ }) | Select-Object -First 1
    foreach ($class in $xml.SelectNodes('//class')) {
        $file = $class.filename.Replace('\', '/')
        if ($source) {
            $file = [IO.Path]::GetFullPath((Join-Path $source $file)).Replace('\', '/')
        }
        $group = if ($class.name -like 'NaxcivanCS.Shared.*') { 'Shared' }
        elseif ($class.name -like 'NaxcivanCS.Backend.Api.*' -or $class.name -eq 'Program') { 'Backend' }
        elseif ($class.name -like 'NaxcivanCS.Server.Players.*' -or $class.name -like 'NaxcivanCS.Server.Damage.*') { 'Server' }
        else { continue }
        foreach ($line in $class.SelectNodes('lines/line')) {
            # Merge repeated observations of a source line across test hosts/classes.
            $key = "$file`:$($line.number)"
            $groups[$group][$key] = $groups[$group][$key] -or ([long]$line.hits -gt 0)
        }
    }
}
$failed = $false
$summary = @('# Line coverage', '', '| Scope | Covered | Total | Coverage | Minimum |', '|---|---:|---:|---:|---:|')
foreach ($name in @('Shared', 'Backend', 'Server')) {
    $total = $groups[$name].Count
    $covered = @($groups[$name].Values | Where-Object { $_ }).Count
    $percent = if ($total) { 100.0 * $covered / $total } else { 0 }
    $summary += "| $name | $covered | $total | $($percent.ToString('F2', [cultureinfo]::InvariantCulture))% | $Threshold% |"
    if (-not $total -or $percent -lt $Threshold) { $failed = $true }
}
$summary | Set-Content -LiteralPath (Join-Path $ResultsDirectory 'coverage-summary.md')
$summary | Write-Output
if ($env:GITHUB_STEP_SUMMARY) { $summary | Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY }
if ($failed) { throw 'Missing coverage or line coverage below threshold.' }
