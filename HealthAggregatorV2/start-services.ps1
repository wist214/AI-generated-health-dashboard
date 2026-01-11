# Start Health Aggregator V2 Services

Write-Host "Starting Health Aggregator V2 Services..." -ForegroundColor Green

# Start API
Write-Host "`n1. Starting API on port 5100..." -ForegroundColor Cyan
Push-Location "$PSScriptRoot\src\Api"
Start-Job -ScriptBlock {
    Set-Location $using:PWD
    dotnet run
} -Name "HealthAggregatorAPI"
Pop-Location

# Wait for API to start
Start-Sleep -Seconds 5

# Start Functions
Write-Host "`n2. Starting Functions on port 7071..." -ForegroundColor Cyan
Push-Location "$PSScriptRoot\src\Functions"
Start-Job -ScriptBlock {
    Set-Location $using:PWD
    func start
} -Name "HealthAggregatorFunctions"
Pop-Location

# Wait for Functions to start
Start-Sleep -Seconds 10

Write-Host "`n✅ Services started!" -ForegroundColor Green
Write-Host "  - API: http://localhost:5100" -ForegroundColor White
Write-Host "  - Functions: http://localhost:7071" -ForegroundColor White
Write-Host "`nTo view logs, use: Get-Job | Receive-Job -Keep" -ForegroundColor Yellow
Write-Host "To stop services, use: Get-Job | Stop-Job; Get-Job | Remove-Job" -ForegroundColor Yellow
