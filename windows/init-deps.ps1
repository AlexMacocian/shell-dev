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

# Install .NET global tools from deps-dotnet.txt
$dotnetDepsFile = Join-Path $PSScriptRoot "deps-dotnet.txt"
if (Test-Path $dotnetDepsFile) {
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        Write-Host "`nInstalling .NET global tools..." -ForegroundColor Cyan

        # Ensure ~/.dotnet/tools is in user PATH
        $dotnetToolsPath = [System.IO.Path]::Combine($env:USERPROFILE, ".dotnet", "tools")
        $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
        if ($userPath -notlike "*$dotnetToolsPath*") {
            Write-Host "Adding $dotnetToolsPath to user PATH..." -ForegroundColor Cyan
            [Environment]::SetEnvironmentVariable("PATH", "$dotnetToolsPath;$userPath", "User")
            $env:PATH = "$dotnetToolsPath;$env:PATH"
        }
        Get-Content $dotnetDepsFile | ForEach-Object {
            $tool = $_.Trim()
            if ($tool -and -not $tool.StartsWith('#')) {
                $installed = dotnet tool list -g 2>$null | Select-String -Pattern $tool
                if ($installed) {
                    Write-Host "$tool is already installed, updating..." -ForegroundColor Yellow
                    dotnet tool update -g $tool
                } else {
                    Write-Host "Installing $tool..." -ForegroundColor Cyan
                    dotnet tool install -g $tool
                }
            }
        }
    } else {
        Write-Host "WARNING: dotnet not found. Skipping .NET global tool installs." -ForegroundColor Red
    }
}

Write-Host "`nDependency setup complete!" -ForegroundColor Green
