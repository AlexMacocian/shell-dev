#!/usr/bin/env pwsh
# Setup script for Neovim configuration on Windows
# Creates symlinks for:
#   - Config: $env:LOCALAPPDATA\nvim -> .config\nvim
#   - Data:   $env:LOCALAPPDATA\nvim-data -> $HOME\.local\share\nvim

# Self-elevate to admin if not already
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process pwsh -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

# $PSScriptRoot may be empty if script is dot-sourced or run via stdin
if ([string]::IsNullOrEmpty($PSScriptRoot)) {
    $RepoRoot = (Get-Location).Path
} else {
    $RepoRoot = $PSScriptRoot
}

$ConfigSource = Join-Path $RepoRoot ".config\nvim"
$ConfigTarget = Join-Path $env:LOCALAPPDATA "nvim"

$DataSource = Join-Path $HOME ".local\share\nvim"
$DataTarget = Join-Path $env:LOCALAPPDATA "nvim-data"

Write-Host "RepoRoot: $RepoRoot" -ForegroundColor Gray

function New-SymlinkIfNeeded {
    param(
        [string]$Source,
        [string]$Target,
        [string]$Name
    )

    if (Test-Path $Target) {
        $item = Get-Item $Target -Force
        if ($item.LinkType -eq "SymbolicLink") {
            Write-Host "$Name symlink already exists at: $Target" -ForegroundColor Yellow
            Write-Host "  Target: $($item.Target)" -ForegroundColor Yellow
            return
        } else {
            Write-Error "$Name - A file or folder already exists at $Target. Please back it up and remove it first."
            return
        }
    }

    # Ensure source directory exists
    if (-not (Test-Path $Source)) {
        Write-Host "Creating directory: $Source" -ForegroundColor Gray
        New-Item -ItemType Directory -Path $Source -Force | Out-Null
    }

    Write-Host "Creating $Name symlink..." -ForegroundColor Cyan
    Write-Host "  Source: $Source" -ForegroundColor Gray
    Write-Host "  Target: $Target" -ForegroundColor Gray
    New-Item -ItemType SymbolicLink -Path $Target -Target $Source | Out-Null
    Write-Host "$Name symlink created!" -ForegroundColor Green
}

# Verify config source exists
if (-not (Test-Path $ConfigSource)) {
    Write-Error "Could not find .config\nvim in repository root: $RepoRoot"
    exit 1
}

New-SymlinkIfNeeded -Source $ConfigSource -Target $ConfigTarget -Name "Config"
New-SymlinkIfNeeded -Source $DataSource -Target $DataTarget -Name "Data"

Write-Host "`nSetup complete!" -ForegroundColor Green

