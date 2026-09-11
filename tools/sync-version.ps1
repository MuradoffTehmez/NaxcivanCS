param(
    [switch]$Check,
    [string]$ReleaseTag
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
[xml]$props = Get-Content -LiteralPath "$root/Directory.Build.props" -Raw
$build = [string]$props.Project.PropertyGroup.NaxcivanCSVersion
$stable = [string]$props.Project.PropertyGroup.NaxcivanCSStableVersion
$date = [string]$props.Project.PropertyGroup.NaxcivanCSReleaseDate
if ($build -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$' -or $stable -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Invalid version in Directory.Build.props'
}
$numeric = $build.Split('-')[0]
$sdk = (Get-Content "$root/global.json" -Raw | ConvertFrom-Json).sdk.version
$constants = Get-Content "$root/shared/NaxcivanCS.Shared/Constants/GameConstants.cs" -Raw
$protocol = [regex]::Match($constants, 'ProtocolVersion = (\d+);').Groups[1].Value
if (-not $protocol) { throw 'Missing protocol version.' }
if ($ReleaseTag) {
    if ($ReleaseTag -cne $build -or $build -cne $stable) {
        throw 'A stable release tag must equal both build and stable versions; prerelease builds cannot be published.'
    }
    if ((Get-Content "$root/CHANGELOG.md" -Raw) -notmatch "(?m)^## $([regex]::Escape($stable)) — $date$") {
        throw 'The release requires a dated CHANGELOG entry.'
    }
}

$updates = @(
    @('shared/NaxcivanCS.Shared/Constants/GameConstants.cs', '(?m)(public const string (?:GameVersion|ContentVersion) = ")[^"]+(";)', "`${1}$build`${2}"),
    @('client/project.godot', '(?m)^config/version="[^"]+"', "config/version=`"$build`""),
    @('server/project.godot', '(?m)^config/version="[^"]+"', "config/version=`"$build`""),
    @('client/export_presets.cfg', '(?m)^(application/(?:product|file)_version=")[^"]+"', "`${1}$numeric`""),
    @('CITATION.cff', '(?m)^version: "[^"]+"', "version: `"$stable`""),
    @('CITATION.cff', '(?m)^date-released: "[^"]+"', "date-released: `"$date`""),
    @('README.md', '(?m)^\*\*Stable release:.*$', "**Stable release: $stable** · **main build: $build$(if ($build -ne $stable) { ' (unreleased)' })** · **Protokol: $protocol**"),
    @('infrastructure/docker/backend.dockerfile', '(?m)^ARG DOTNET_SDK_VERSION=.*$', "ARG DOTNET_SDK_VERSION=$sdk"),
    @('infrastructure/docker/gameserver.dockerfile', '(?m)^ARG DOTNET_SDK_VERSION=.*$', "ARG DOTNET_SDK_VERSION=$sdk"),
    @('SECURITY.md', '(?m)^\| [\d.]+ \| :white_check_mark:.*$', "| $stable | :white_check_mark: | Cari stable buraxılış |"),
    @('SECURITY.md', '(?m)^\| < [\d.]+ \|.*$', "| < $stable | :x: | Köhnə prototiplər üçün dəstək verilmir |")
)
$stale = @()
foreach ($update in $updates) {
    $path, $pattern, $replacement = $update
    $fullPath = Join-Path $root $path
    $content = [IO.File]::ReadAllText($fullPath).Replace("`r`n", "`n")
    if ($content -notmatch $pattern) { throw "Version marker missing: $path" }
    $expected = [regex]::Replace($content, $pattern, $replacement)
    if ($expected -cne $content) {
        $stale += $path
        if (-not $Check) { [IO.File]::WriteAllText($fullPath, $expected) }
    }
}
if ($Check -and $stale.Count) { throw "Version drift: $($stale -join ', '). Run tools/sync-version.ps1." }
Write-Host "Versions: stable=$stable, build=$build; synchronized."
