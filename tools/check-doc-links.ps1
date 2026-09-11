#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$failures = [System.Collections.Generic.List[string]]::new()
if (Get-Command rg -ErrorAction SilentlyContinue) {
    $files = @(& rg --files --hidden -g '*.md' -g '!**/.git/**' $repoRoot)
} else {
    # GitHub-hosted images do not necessarily include ripgrep.
    $relativeFiles = @(& git -C $repoRoot -c core.quotepath=false ls-files --cached --others --exclude-standard -- '*.md')
    $files = @($relativeFiles | ForEach-Object { Join-Path $repoRoot $_ })
}
if ($LASTEXITCODE -ne 0 -or !$files.Count) { throw 'Could not enumerate Markdown files.' }
$linkCount = 0
foreach ($file in $files) {
    $inFence = $false
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadLines($file)) {
        $lineNumber++
        if ($line -match '^\s*```') { $inFence = !$inFence; continue }
        if ($inFence) { continue }
        foreach ($match in [regex]::Matches($line, '\[[^\]]*\]\(([^)]+)\)')) {
            $target = $match.Groups[1].Value.Trim().Trim('<', '>')
            if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $target.StartsWith('#')) { continue }
            $target = [uri]::UnescapeDataString(($target -split '#', 2)[0])
            if (!$target) { continue }
            $resolved = [System.IO.Path]::GetFullPath((Join-Path (Split-Path $file) $target))
            $linkCount++
            if (!(Test-Path -LiteralPath $resolved)) {
                $failures.Add("${file}:${lineNumber}: missing $target")
            }
        }
    }
    if ($inFence) { $failures.Add("${file}: unclosed code fence") }
}
if ($failures.Count) { $failures | Write-Output; throw "$($failures.Count) documentation errors." }
Write-Output "OK: $($files.Count) Markdown files, $linkCount local links; code fences balanced."
Write-Output 'External URLs and heading anchors are not checked.'
