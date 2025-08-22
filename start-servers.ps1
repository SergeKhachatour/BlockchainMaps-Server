# Blockchain Maps Backend Servers Startup Script
# PowerShell Version

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Blockchain Maps Backend Servers" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✓ Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Node.js not found. Please install Node.js first." -ForegroundColor Red
    Write-Host "Download from: https://nodejs.org/" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Check if npm packages are installed
if (-not (Test-Path "node_modules")) {
    Write-Host "Installing npm packages..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Failed to install packages" -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
}

Write-Host ""
Write-Host "Starting servers..." -ForegroundColor Green

# Start Stellar Backend
Write-Host "Starting Stellar Backend (Port 3000)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run start:stellar" -WindowStyle Normal

# Wait a moment
Start-Sleep -Seconds 2

# Start Marker Backend
Write-Host "Starting Marker Backend (Port 3001)..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run start:marker" -WindowStyle Normal

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "    Servers are starting..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Stellar Backend: http://localhost:3000" -ForegroundColor White
Write-Host "Marker Backend:  http://localhost:3001" -ForegroundColor White
Write-Host ""
Write-Host "Health Checks:" -ForegroundColor White
Write-Host "  - Stellar: http://localhost:3000/health" -ForegroundColor Gray
Write-Host "  - Marker:  http://localhost:3001/health" -ForegroundColor Gray
Write-Host ""
Write-Host "Unity Integration:" -ForegroundColor White
Write-Host "  - Open Unity project" -ForegroundColor Gray
Write-Host "  - Run the BlockchainMapServer scene" -ForegroundColor Gray
Write-Host "  - Check Console for initialization messages" -ForegroundColor Gray
Write-Host ""
Write-Host "Press any key to close this window..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 