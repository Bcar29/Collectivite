# Script pour preparer la publication et creer l'installeur
# Usage: .\build_installer.ps1

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Preparation de l'installeur Collectivite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Chemins
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectPath = Join-Path $ProjectRoot "Collectivite\Collectivite.csproj"
$OutputDir = Join-Path $ProjectRoot "installer\publish"
$InnoScript = Join-Path $PSScriptRoot "setup.iss"

# Nettoyer les anciens fichiers
if (Test-Path $OutputDir) {
    Write-Host "Nettoyage des anciens fichiers..." -ForegroundColor Yellow
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Publication de l'application (self-contained)
Write-Host "Publication de l'application..." -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Runtime: $Runtime" -ForegroundColor Gray
Write-Host ""

Set-Location $ProjectRoot

dotnet publish $ProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erreur lors de la publication!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Publication terminee avec succes!" -ForegroundColor Green
Write-Host "Fichiers publics dans: $OutputDir" -ForegroundColor Gray
Write-Host ""

# Verification des fichiers essentiels
$exePath = Join-Path $OutputDir "Collectivite.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERREUR: Collectivite.exe non trouve!" -ForegroundColor Red
    exit 1
}

Write-Host "Fichiers prepares pour l'installeur:" -ForegroundColor Green
Get-ChildItem $OutputDir | Select-Object Name, @{Name="Taille (MB)";Expression={[math]::Round($_.Length/1MB, 2)}} | Format-Table -AutoSize

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Etapes suivantes:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "1. Installer Inno Setup Compiler (si pas deja installe)"
Write-Host "   Telechargement: https://jrsoftware.org/isdl.php"
Write-Host ""
Write-Host "2. Compiler l'installeur avec Inno Setup:"
Write-Host "   - Ouvrir setup.iss dans Inno Setup Compiler"
Write-Host "   - Ou executer: `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe`" setup.iss"
Write-Host ""
Write-Host "3. L'installeur sera cree dans: installer\Output\" -ForegroundColor Yellow
Write-Host ""

