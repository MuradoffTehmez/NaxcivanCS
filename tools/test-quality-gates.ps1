$ErrorActionPreference = 'Stop'
$scripts = $PSScriptRoot
$licensePath = Join-Path (Split-Path $scripts -Parent) 'LICENSE'
# Canonical GitHub/Choose a License GPL v3 text; normalize checkout line endings.
$license = [IO.File]::ReadAllText($licensePath).Replace("`r`n", "`n")
$licenseHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($license)))
if ($licenseHash -ne '3972DC9744F6499F0F9B2DBF76696F2AE7AD8AF9B23DDE66D6AF86C9DFB36986') {
    throw 'LICENSE must retain the canonical GPL v3 text; project grant is GPL-3.0-or-later.'
}
$repoRoot = Split-Path $scripts -Parent
[xml]$metadata = Get-Content "$repoRoot/Directory.Build.props" -Raw
if ($metadata.Project.PropertyGroup.PackageLicenseExpression -ne 'GPL-3.0-or-later' -or
    (Get-Content "$repoRoot/CITATION.cff" -Raw) -notmatch '(?m)^license: "GPL-3.0-or-later"\r?$') {
    throw 'Project license metadata must be GPL-3.0-or-later.'
}
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "naxcivancs-gates-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $tempRoot | Out-Null

function Assert-Fails([scriptblock]$Action) {
    $failed = $false
    try { & $Action | Out-Null } catch { $failed = $true }
    if (-not $failed) { throw 'Expected quality gate to reject the fixture.' }
}

try {
    # Coverage must fail closed; multiple reports must merge line observations.
    $results = Join-Path $tempRoot 'coverage'
    New-Item -ItemType Directory -Path $results | Out-Null
    Assert-Fails { & "$scripts/check-coverage.ps1" -ResultsDirectory $results }
    $classes = @('NaxcivanCS.Shared.Example', 'NaxcivanCS.Backend.Api.Example', 'NaxcivanCS.Server.Players.Example')
    foreach ($run in 1..2) {
        $dir = Join-Path $results $run
        New-Item -ItemType Directory -Path $dir | Out-Null
        $source = if ($run -eq 1) { Join-Path $tempRoot 'source' } else { $tempRoot }
        $xml = "<coverage><sources><source>$([System.Security.SecurityElement]::Escape($source))</source></sources><packages><package><classes>"
        foreach ($class in $classes) {
            $file = if ($run -eq 1) { "$class.cs" } else { "source/$class.cs" }
            $xml += "<class name=`"$class`" filename=`"$file`"><lines>"
            foreach ($line in 1..10) {
                $hits = if (($run -eq 1 -and $line -le 4) -or ($run -eq 2 -and $line -ge 5 -and $line -le 7)) { 1 } else { 0 }
                $xml += "<line number=`"$line`" hits=`"$hits`" />"
            }
            $xml += '</lines></class>'
        }
        $xml += '</classes></package></packages></coverage>'
        $xml | Set-Content -LiteralPath "$dir/coverage.cobertura.xml"
    }
    & "$scripts/check-coverage.ps1" -ResultsDirectory $results
    Assert-Fails { & "$scripts/check-coverage.ps1" -ResultsDirectory $results -Threshold 71 }

    # DCO checks the complete PR range, even when the newest commit is signed.
    Push-Location $tempRoot
    try {
        git init --quiet
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false commit --allow-empty -m base --quiet
        $base = git rev-parse HEAD
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false commit --allow-empty -s -m signed --quiet
        $signed = git rev-parse HEAD
        & "$scripts/check-dco.ps1" -BaseSha $base -HeadSha $signed
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false commit --allow-empty -m unsigned --quiet
        $unsigned = git rev-parse HEAD
        Assert-Fails { & "$scripts/check-dco.ps1" -BaseSha $base -HeadSha $unsigned }
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false commit --allow-empty -s -m signed-again --quiet
        $head = git rev-parse HEAD
        Assert-Fails { & "$scripts/check-dco.ps1" -BaseSha $base -HeadSha $head }

        # An unsigned merge commit is allowed: GitHub's merge button cannot sign
        # it, and it introduces no authored content of its own.
        git checkout --quiet -b gate-topic $signed
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false commit --allow-empty -s -m topic-signed --quiet
        git checkout --quiet $signed
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false merge --no-ff --no-edit -m merge-unsigned gate-topic --quiet
        $mergedSigned = git rev-parse HEAD
        & "$scripts/check-dco.ps1" -BaseSha $base -HeadSha $mergedSigned

        # ...but the merge must not smuggle in an unsigned content commit.
        git checkout --quiet -b gate-topic-unsigned $signed
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false commit --allow-empty -m topic-unsigned --quiet
        git checkout --quiet $mergedSigned
        git -c user.name='Gate Test' -c user.email='gate@example.invalid' -c commit.gpgsign=false merge --no-ff --no-edit -m merge-smuggler gate-topic-unsigned --quiet
        Assert-Fails { & "$scripts/check-dco.ps1" -BaseSha $base -HeadSha (git rev-parse HEAD) }
    } finally { Pop-Location }
    Assert-Fails { & "$scripts/sync-version.ps1" -Check -ReleaseTag 'invalid-tag' }
    # Run the real version helper in an isolated repo-shaped fixture.
    $versionRoot = Join-Path $tempRoot 'version'
    $repoRoot = Split-Path $scripts -Parent
    $versionFiles = @(
        'Directory.Build.props', 'global.json', 'README.md', 'SECURITY.md', 'CITATION.cff',
        'shared/NaxcivanCS.Shared/Constants/GameConstants.cs', 'client/project.godot',
        'server/project.godot', 'client/export_presets.cfg', 'tools/sync-version.ps1',
        'infrastructure/docker/backend.dockerfile', 'infrastructure/docker/gameserver.dockerfile'
    )
    foreach ($file in $versionFiles) {
        $destination = Join-Path $versionRoot $file
        New-Item -ItemType Directory -Force -Path (Split-Path $destination -Parent) | Out-Null
        Copy-Item -LiteralPath (Join-Path $repoRoot $file) -Destination $destination
    }
    & "$versionRoot/tools/sync-version.ps1" -Check
    $godotProject = "$versionRoot/client/project.godot"
    (Get-Content $godotProject -Raw) -replace 'config/version="[^"]+"', 'config/version="9.9.9"' | Set-Content $godotProject
    Assert-Fails { & "$versionRoot/tools/sync-version.ps1" -Check }
    & "$versionRoot/tools/sync-version.ps1"
    & "$versionRoot/tools/sync-version.ps1" -Check
    Write-Host 'Quality gate regression checks passed.'
} finally {
    # Delete only the verified, uniquely-created fixture directory.
    $resolved = [IO.Path]::GetFullPath($tempRoot)
    $parent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ((Split-Path $resolved -Parent) -ne $parent -or (Split-Path $resolved -Leaf) -notlike 'naxcivancs-gates-*') {
        throw 'Unexpected fixture cleanup path.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
