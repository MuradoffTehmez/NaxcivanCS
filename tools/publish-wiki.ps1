#requires -Version 7.0
[CmdletBinding()]
param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._/-]*$')]
    [string]$SourceRef = 'main',
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $repoRoot 'docs/wiki'
$output = Join-Path $repoRoot 'artifacts/wiki'
$repoUrl = 'https://github.com/MuradoffTehmez/NaxcivanCS'
$wikiRemote = "$repoUrl.wiki.git"
$utf8 = [System.Text.UTF8Encoding]::new($false)
$pages = @(Get-ChildItem -LiteralPath $source -Filter '*.md' -File)
if (!$pages.Count) { throw 'No Wiki source pages found.' }
[System.IO.Directory]::CreateDirectory($output) | Out-Null

foreach ($page in $pages) {
    $content = [System.IO.File]::ReadAllText($page.FullName)
    $content = [regex]::Replace($content, '(\[[^\]]*\]\()([^)]+)(\))', {
        param($match)
        $target = $match.Groups[2].Value
        if ($target -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $target.StartsWith('#')) { return $match.Value }
        $parts = $target -split '#', 2
        $local = [System.IO.Path]::GetFullPath((Join-Path $source $parts[0]))
        $fragment = if ($parts.Count -eq 2) { '#' + $parts[1] } else { '' }
        if (!(Test-Path -LiteralPath $local)) { throw "Missing Wiki link: $target" }
        if ((Split-Path $local) -eq $source -and [System.IO.Path]::GetExtension($local) -eq '.md') {
            $url = "$repoUrl/wiki/" + [uri]::EscapeDataString([System.IO.Path]::GetFileNameWithoutExtension($local))
        } else {
            $relative = [System.IO.Path]::GetRelativePath($repoRoot, $local).Replace('\', '/')
            if ($relative.StartsWith('../') -or [System.IO.Path]::IsPathRooted($relative)) { throw 'Link escapes repository.' }
            $kind = if (Test-Path -LiteralPath $local -PathType Container) { 'tree' } else { 'blob' }
            $encodedPath = ($relative.Split('/') | ForEach-Object { [uri]::EscapeDataString($_) }) -join '/'
            $url = "$repoUrl/$kind/" + [uri]::EscapeDataString($SourceRef) + '/' + $encodedPath
        }
        return $match.Groups[1].Value + $url + $fragment + $match.Groups[3].Value
    })
    [System.IO.File]::WriteAllText((Join-Path $output $page.Name), $content, $utf8)
}
Write-Output "Exported $($pages.Count) Wiki files to $output"
if (!$Publish) { Write-Output 'Preview only. Add -Publish to commit and push the Wiki.'; return }

& git -C $repoRoot rev-parse --verify "$SourceRef^{commit}" | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'SourceRef must resolve locally before publishing.' }
$authorName = & git -C $repoRoot config user.name
$authorEmail = & git -C $repoRoot config user.email
if (!$authorName -or !$authorEmail) { throw 'Configure author name/email in the main repository before publishing.' }
$clone = Join-Path $repoRoot ('artifacts/wiki-publish-' + [guid]::NewGuid().ToString('N'))
& git clone -- $wikiRemote $clone
if ($LASTEXITCODE -ne 0) {
    throw 'Wiki clone failed. Check GitHub access and create the first Home page in the GitHub Wiki UI if it has not been initialized.'
}
foreach ($page in $pages) {
    Copy-Item -LiteralPath (Join-Path $output $page.Name) -Destination (Join-Path $clone $page.Name)
    & git -C $clone add -- $page.Name
    if ($LASTEXITCODE -ne 0) { throw 'Wiki staging failed.' }
}
& git -C $clone diff --cached --quiet
if ($LASTEXITCODE -eq 0) { Write-Output 'Wiki is already up to date.'; return }
if ($LASTEXITCODE -ne 1) { throw 'Could not inspect staged Wiki changes.' }
& git -C $clone -c "user.name=$authorName" -c "user.email=$authorEmail" commit -m "docs: publish NaxcivanCS $SourceRef Wiki"
if ($LASTEXITCODE -ne 0) { throw 'Wiki commit failed.' }
& git -C $clone push origin HEAD
if ($LASTEXITCODE -ne 0) { throw 'Wiki push failed; clone retained for inspection.' }
Write-Output "Published: $repoUrl/wiki"
Write-Output "Wiki checkout retained at: $clone"
