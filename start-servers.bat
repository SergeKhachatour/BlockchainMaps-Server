@echo off
echo ========================================
echo    Blockchain Maps Backend Servers
echo ========================================
echo.

echo Starting Stellar Backend (Port 3000)...
start "Stellar Backend" cmd /k "npm run start:stellar"

echo Starting Marker Backend (Port 3001)...
start "Marker Backend" cmd /k "npm run start:marker"

echo.
echo ========================================
echo    Servers are starting...
echo ========================================
echo.
echo Stellar Backend: http://localhost:3000
echo Marker Backend:  http://localhost:3001
echo.
echo Health Checks:
echo   - Stellar: http://localhost:3000/health
echo   - Marker:  http://localhost:3001/health
echo.
echo Press any key to close this window...
pause > nul 