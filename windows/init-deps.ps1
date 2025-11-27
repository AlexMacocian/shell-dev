#!/usr/bin/env pwsh
# Dependency initialization script for Neovim setup on Windows
# Installs all packages listed in deps.txt via winget

$depsFile = Join-Path $PSScriptRoot "deps.txt"
if (-not (Test-Path $depsFile)) {
    Write-Host "Dependency file deps.txt not found!" -ForegroundColor Red
    exit 1
}

# Self-elevate to admin if not already
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process pwsh -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

function Install-IfMissing {
    param([string]$PackageId)
    Write-Host "Checking for $PackageId..." -ForegroundColor Gray
    $installed = winget list --id $PackageId | Select-String $PackageId
    if ($installed) {
        Write-Host "$PackageId is already installed." -ForegroundColor Yellow
    } else {
        Write-Host "Installing $PackageId..." -ForegroundColor Cyan
        winget install --id $PackageId --silent
        Write-Host "$PackageId installation complete!" -ForegroundColor Green
    }
}

Get-Content $depsFile | ForEach-Object {
    $dep = $_.Trim()
    if ($dep) { Install-IfMissing -PackageId $dep }
}

Write-Host "`nDependency setup complete!" -ForegroundColor Green
