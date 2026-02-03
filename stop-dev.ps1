<#
.SYNOPSIS
    Stop Egibi dev environment.

.EXAMPLE
    .\stop-dev.ps1           # Stop containers (keep data)
    .\stop-dev.ps1 -Reset    # Stop containers AND delete all data volumes
#>

param(
    [switch]$Reset
)

$root = $PSScriptRoot

Write-Host ""
Write-Host "Stopping Egibi containers..." -ForegroundColor Yellow

if ($Reset) {
    Write-Host "  Removing data volumes (full reset)..." -ForegroundColor Red
    docker compose -f "$root\docker-compose.yml" down -v
} else {
    docker compose -f "$root\docker-compose.yml" down
}

Write-Host "Done." -ForegroundColor Green
Write-Host ""
