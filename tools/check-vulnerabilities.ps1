param([string]$OutputDirectory = 'artifacts/security')

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$targets = @('NaxcivanCS.sln', 'server/NaxcivanCS.Server.csproj', 'client/NaxcivanCS.Client.csproj')
$failed = $false
foreach ($target in $targets) {
    dotnet restore $target
    if ($LASTEXITCODE) { throw "Restore failed: $target" }
    $json = dotnet list $target package --vulnerable --include-transitive --format json --output-version 1
    if ($LASTEXITCODE) { throw "Package audit failed: $target" }
    $json | Set-Content -LiteralPath (Join-Path $OutputDirectory "$([IO.Path]::GetFileNameWithoutExtension($target)).json")
    $report = $json | ConvertFrom-Json
    if (-not $report.projects -or $report.problems) {
        throw "Invalid or incomplete audit: $target"
    }
    foreach ($project in $report.projects) {
        foreach ($framework in $project.frameworks) {
            foreach ($package in @($framework.topLevelPackages) + @($framework.transitivePackages)) {
                if ($package.vulnerabilities) {
                    Write-Output "Vulnerable package: $($package.id) $($package.resolvedVersion) ($target)"
                    $failed = $true
                }
            }
        }
    }
}
if ($failed) { throw 'Vulnerable packages found; see JSON audit artifacts.' }
Write-Host 'No known vulnerable packages in solution, client or server.'
