param(
    [Parameter(Mandatory)][string]$BaseSha,
    [Parameter(Mandatory)][string]$HeadSha
)

$ErrorActionPreference = 'Stop'
foreach ($sha in @($BaseSha, $HeadSha)) {
    if ($sha -notmatch '^[0-9a-fA-F]{40}$') { throw 'Expected a full commit SHA.' }
    git cat-file -e "$sha^{commit}"
    if ($LASTEXITCODE) { throw "Commit unavailable: $sha" }
}
# Merge commits carry no new authored content and cannot be signed through
# GitHub's merge button, which would permanently block develop -> main.
# Everything a merge brings in is still listed individually by rev-list,
# so each content-bearing commit is verified.
$commits = @(git rev-list --no-merges "$BaseSha..$HeadSha")
if ($LASTEXITCODE) { throw 'Cannot enumerate PR commits.' }
$unsigned = @()
foreach ($commit in $commits) {
    $author = git show -s --format='%an <%ae>' $commit
    $trailers = @(git show -s '--format=%(trailers:key=Signed-off-by,valueonly)' $commit)
    if ($LASTEXITCODE) { throw "Cannot read commit: $commit" }
    if (-not ($trailers | Where-Object { $_.Trim() -ceq $author.Trim() })) {
        $unsigned += $commit
        Write-Output "Missing author Signed-off-by trailer: $commit"
    }
}
if ($unsigned.Count) { throw 'DCO failed. Each non-merge PR commit needs its author sign-off; see CONTRIBUTING.md.' }
Write-Output "DCO passed for $($commits.Count) non-merge PR commits."
