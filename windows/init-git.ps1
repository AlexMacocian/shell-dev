#!/usr/bin/env pwsh
# Git and SSH setup for GitHub with YubiKey FIDO2 keys

# Self-elevate to admin if not already (needed for OpenSSH agent service)
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process pwsh -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

if ([string]::IsNullOrEmpty($PSScriptRoot)) {
    $ScriptDir = (Get-Location).Path
} else {
    $ScriptDir = $PSScriptRoot
}
$RepoRoot = Split-Path $ScriptDir -Parent
$GitDir = Join-Path $RepoRoot "git"

Write-Host "Setting up git and SSH for GitHub..." -ForegroundColor Cyan

# --- SSH key setup ---
$SshDir = Join-Path $env:USERPROFILE ".ssh"
if (-not (Test-Path $SshDir)) {
    New-Item -ItemType Directory -Path $SshDir -Force | Out-Null
}

# Copy public keys
Copy-Item (Join-Path $GitDir "yubikey-touch.pub") (Join-Path $SshDir "yubikey-touch.pub") -Force
Copy-Item (Join-Path $GitDir "yubikey-nfc.pub") (Join-Path $SshDir "yubikey-nfc.pub") -Force

# Install SSH config (back up existing)
$SshConfig = Join-Path $SshDir "config"
if (Test-Path $SshConfig) {
    $backup = "$SshConfig.backup.$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Write-Host "Backing up existing SSH config: $SshConfig -> $backup" -ForegroundColor Yellow
    Copy-Item $SshConfig $backup
}
Copy-Item (Join-Path $GitDir "ssh-config") $SshConfig -Force

# --- Extract private key stubs from YubiKey ---
Write-Host ""
Write-Host "Extracting resident SSH keys from YubiKey..." -ForegroundColor Cyan
Write-Host "You will be prompted for your FIDO2 PIN and may need to touch the key."

$TempDir = Join-Path $env:TEMP "ssh-keygen-extract-$(Get-Random)"
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

try {
    $result = & ssh-keygen -K -f (Join-Path $TempDir "id_sk") 2>&1
    if ($LASTEXITCODE -eq 0) {
        $TouchFP = (ssh-keygen -lf (Join-Path $SshDir "yubikey-touch.pub") | Select-String -Pattern 'SHA256:\S+').Matches.Value
        $NfcFP = (ssh-keygen -lf (Join-Path $SshDir "yubikey-nfc.pub") | Select-String -Pattern 'SHA256:\S+').Matches.Value

        Get-ChildItem (Join-Path $TempDir "id_sk_rk_*") -Exclude "*.pub" -ErrorAction SilentlyContinue | ForEach-Object {
            $keyFP = (ssh-keygen -lf $_.FullName | Select-String -Pattern 'SHA256:\S+').Matches.Value
            if ($keyFP -eq $TouchFP) {
                Copy-Item $_.FullName (Join-Path $SshDir "yubikey-touch") -Force
                Write-Host "Installed yubikey-touch private key stub" -ForegroundColor Green
            } elseif ($keyFP -eq $NfcFP) {
                Copy-Item $_.FullName (Join-Path $SshDir "yubikey-nfc") -Force
                Write-Host "Installed yubikey-nfc private key stub" -ForegroundColor Green
            }
        }
    } else {
        Write-Host "Could not extract keys from YubiKey (wrong PIN or no key present)." -ForegroundColor Yellow
        Write-Host "You can manually place the private key stubs at:" -ForegroundColor Yellow
        Write-Host "  $SshDir\yubikey-touch"
        Write-Host "  $SshDir\yubikey-nfc"
    }
} finally {
    Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue
}

# --- Git commit signing ---
git config --global gpg.format ssh
git config --global user.signingkey (Join-Path $SshDir "yubikey-touch.pub")
git config --global commit.gpgsign true
git config --global tag.gpgsign true

# --- Ensure OpenSSH agent is running ---
Write-Host ""
Write-Host "Configuring OpenSSH Authentication Agent..." -ForegroundColor Cyan
Set-Service -Name ssh-agent -StartupType Automatic -ErrorAction SilentlyContinue
Start-Service ssh-agent -ErrorAction SilentlyContinue

$touchKey = Join-Path $SshDir "yubikey-touch"
$nfcKey = Join-Path $SshDir "yubikey-nfc"
if (Test-Path $touchKey) { ssh-add $touchKey 2>$null; Write-Host "Added yubikey-touch" }
if (Test-Path $nfcKey) { ssh-add $nfcKey 2>$null; Write-Host "Added yubikey-nfc" }

Write-Host ""
Write-Host "Git and SSH setup complete!" -ForegroundColor Green
Write-Host "Test with: ssh -T git@github.com"
