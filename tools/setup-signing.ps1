#requires -Version 7.0
[CmdletBinding()]
param()

$sshDir = Join-Path $env:USERPROFILE '.ssh'
if (!(Test-Path -LiteralPath $sshDir)) {
    New-Item -ItemType Directory -Path $sshDir -Force | Out-Null
}

$privateKey = Join-Path $sshDir 'id_ed25519_signing'
$publicKey = "$privateKey.pub"

if (!(Test-Path -LiteralPath $privateKey)) {
    Write-Host "SSH imzalama acari yaradilir: $privateKey"
    & ssh-keygen -t ed25519 -C 'muradofftehmez01@gmail.com' -f $privateKey -N ''
    if ($LASTEXITCODE -ne 0) { throw 'ssh-keygen ugursuz oldu' }
} else {
    Write-Host "Movcud SSH imzalama acari tapildi: $privateKey"
}

$pubKeyContent = (Get-Content -LiteralPath $publicKey -Raw).Trim()

# Git konfiqurasiyasi
$normalizedPubKeyPath = $publicKey.Replace('\', '/')
& git config --global gpg.format ssh
& git config --global user.signingkey $normalizedPubKeyPath
& git config --global commit.gpgsign true
& git config --global tag.gpgsign true

Write-Host "=========================================="
Write-Host "Git SSH imzalama ugurla konfiqurasiya edildi!"
Write-Host "Key yolu: $normalizedPubKeyPath"
Write-Host "gpg.format: ssh"
Write-Host "commit.gpgsign: true"
Write-Host "tag.gpgsign: true"
Write-Host "=========================================="
Write-Host "GITHUB-A ELAVE EDILMELI ACIQ ACAR (PUBLIC KEY):"
Write-Host $pubKeyContent
Write-Host "=========================================="
