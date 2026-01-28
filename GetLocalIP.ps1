# PowerShell script to get local IP address
# Run this script to get your computer's IP address for Flutter app

$ipAddress = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" -and $_.IPAddress -notlike "169.254.*" } | Select-Object -First 1).IPAddress

Write-Host ""
Write-Host "=========================================" -ForegroundColor Green
Write-Host "Your Local IP Address: $ipAddress" -ForegroundColor Yellow
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Update Flutter app baseUrl to:" -ForegroundColor Cyan
Write-Host "http://$ipAddress:5053/api" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")



