<#
.SYNOPSIS
    Start Egibi dev environment: databases + API.

.DESCRIPTION
    1. Starts PostgreSQL & QuestDB via Docker Compose
    2. Waits for both containers to be healthy
    3. (Optional) Runs EF Core migrations
    4. Launches the API with dotnet run

.EXAMPLE
    .\start-dev.ps1              # Start DBs + API
    .\start-dev.ps1 -DbOnly     # Start DBs only
    .\start-dev.ps1 -Migrate    # Start DBs, run EF migrations, then API
#>

param(
    [switch]$DbOnly,
    [switch]$Migrate
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Egibi Development Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ── 1. Start Docker containers ──────────────────────────────
Write-Host "[1/4] Starting database containers..." -ForegroundColor Yellow
docker compose -f "$root\docker-compose.yml" up -d
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: docker compose failed. Is Docker Desktop running?" -ForegroundColor Red
    exit 1
}

# ── 2. Wait for healthy ─────────────────────────────────────
Write-Host "[2/4] Waiting for containers to be healthy..." -ForegroundColor Yellow

$maxWait = 60
$elapsed = 0

while ($elapsed -lt $maxWait) {
    $pgHealth = docker inspect --format='{{.State.Health.Status}}' egibi-postgres 2>$null
    $qdbHealth = docker inspect --format='{{.State.Health.Status}}' egibi-questdb 2>$null

    if ($pgHealth -eq "healthy" -and $qdbHealth -eq "healthy") {
        Write-Host "  PostgreSQL:  healthy" -ForegroundColor Green
        Write-Host "  QuestDB:     healthy" -ForegroundColor Green
        break
    }

    $pgStatus = if ($pgHealth) { $pgHealth } else { "starting" }
    $qdbStatus = if ($qdbHealth) { $qdbHealth } else { "starting" }
    Write-Host "  PostgreSQL: $pgStatus | QuestDB: $qdbStatus  ($elapsed`s)" -ForegroundColor Gray
    Start-Sleep -Seconds 2
    $elapsed += 2
}

if ($elapsed -ge $maxWait) {
    Write-Host "WARNING: Timed out waiting for healthy containers. Continuing anyway..." -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host "  Endpoints:" -ForegroundColor Cyan
Write-Host "    PostgreSQL:      localhost:5432  (egibi_app_db)" -ForegroundColor White
Write-Host "    QuestDB PG:      localhost:8812" -ForegroundColor White
Write-Host "    QuestDB Console: http://localhost:9000" -ForegroundColor White
Write-Host "    QuestDB ILP:     localhost:9009" -ForegroundColor White
Write-Host ""

if ($DbOnly) {
    Write-Host "Databases are ready. Use 'docker compose down' to stop." -ForegroundColor Green
    exit 0
}

# ── 3. EF Migrations (optional) ─────────────────────────────
if ($Migrate) {
    Write-Host "[3/4] Running EF Core migrations..." -ForegroundColor Yellow
    Push-Location "$root\egibi-api"
    dotnet ef database update
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: EF migration failed. The API will attempt to run anyway." -ForegroundColor DarkYellow
    } else {
        Write-Host "  Migrations applied successfully." -ForegroundColor Green
    }
    Pop-Location
} else {
    Write-Host "[3/4] Skipping migrations (use -Migrate flag to apply)" -ForegroundColor Gray
}

# ── 4. Launch API ────────────────────────────────────────────
Write-Host "[4/4] Starting egibi-api..." -ForegroundColor Yellow
Write-Host "  API will be at: http://localhost:5170" -ForegroundColor Cyan
Write-Host "  Swagger:        http://localhost:5170/swagger" -ForegroundColor Cyan
Write-Host "  Press Ctrl+C to stop the API" -ForegroundColor Gray
Write-Host ""

Push-Location "$root\egibi-api"
dotnet run --launch-profile http
Pop-Location
