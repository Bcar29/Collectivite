# Script de verification des prerequis avant installation
# Usage: .\check_prerequisites.ps1

$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verification des prerequis - Collectivite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$allOk = $true

# Verification de .NET 8.0 Runtime
Write-Host "1. Verification de .NET 8.0 Runtime..." -ForegroundColor Yellow
$dotnetPath = "C:\Program Files\dotnet\dotnet.exe"
if (Test-Path $dotnetPath) {
    $dotnetVersion = & $dotnetPath --version 2>&1
    if ($dotnetVersion -match "^8\.0") {
        Write-Host "   [OK] .NET 8.0 detecte: $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "   [ERREUR] .NET 8.0 non detecte. Version trouvee: $dotnetVersion" -ForegroundColor Red
        $allOk = $false
    }
} else {
    Write-Host "   [ERREUR] .NET Runtime non installe" -ForegroundColor Red
    Write-Host "   Telechargez depuis: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    $allOk = $false
}

Write-Host ""

# Verification de MySQL/MariaDB
Write-Host "2. Verification de MySQL/MariaDB..." -ForegroundColor Yellow
$mysqlFound = $false

# Verifier MySQL
$mysqlPaths = @(
    "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe",
    "C:\Program Files\MySQL\MySQL Server 8.1\bin\mysql.exe",
    "C:\Program Files (x86)\MySQL\MySQL Server 8.0\bin\mysql.exe"
)

foreach ($path in $mysqlPaths) {
    if (Test-Path $path) {
        Write-Host "   [OK] MySQL detecte: $path" -ForegroundColor Green
        $mysqlFound = $true
        break
    }
}

# Verifier MariaDB
if (-not $mysqlFound) {
    $mariadbPaths = @(
        "C:\Program Files\MariaDB 10.11\bin\mysql.exe",
        "C:\Program Files\MariaDB 10.10\bin\mysql.exe",
        "C:\Program Files\MariaDB 10.9\bin\mysql.exe"
    )
    
    foreach ($path in $mariadbPaths) {
        if (Test-Path $path) {
            Write-Host "   [OK] MariaDB detecte: $path" -ForegroundColor Green
            $mysqlFound = $true
            break
        }
    }
}

# Verifier via le service Windows
if (-not $mysqlFound) {
    $services = Get-Service | Where-Object { $_.Name -like "*mysql*" -or $_.Name -like "*mariadb*" }
    if ($services) {
        foreach ($service in $services) {
            Write-Host "   [INFO] Service MySQL/MariaDB trouve: $($service.Name) (Status: $($service.Status))" -ForegroundColor Cyan
            $mysqlFound = $true
        }
    }
}

if (-not $mysqlFound) {
    Write-Host "   [ATTENTION] MySQL/MariaDB non detecte automatiquement" -ForegroundColor Yellow
    Write-Host "   Vous devez installer MySQL ou MariaDB avant d'installer l'application" -ForegroundColor Yellow
    Write-Host "   MySQL: https://dev.mysql.com/downloads/mysql/" -ForegroundColor Gray
    Write-Host "   MariaDB: https://mariadb.org/download/" -ForegroundColor Gray
    # Note: On ne bloque pas l'installation car MySQL peut etre installe apres
}

Write-Host ""

# Verification de l'espace disque
Write-Host "3. Verification de l'espace disque..." -ForegroundColor Yellow
$drive = Get-PSDrive C
$freeSpaceGB = [math]::Round($drive.Free / 1GB, 2)
if ($freeSpaceGB -gt 1) {
    Write-Host "   [OK] Espace disque disponible: $freeSpaceGB GB" -ForegroundColor Green
} else {
    Write-Host "   [ATTENTION] Espace disque faible: $freeSpaceGB GB" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
if ($allOk) {
    Write-Host "Tous les prerequis sont satisfaits!" -ForegroundColor Green
    Write-Host "Vous pouvez proceder a l'installation." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Certains prerequis manquent." -ForegroundColor Red
    Write-Host "Veuillez installer les composants manquants avant de continuer." -ForegroundColor Red
    exit 1
}

