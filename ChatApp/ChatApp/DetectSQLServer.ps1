# Detect SQL Server instances and generate connection string
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SQL Server Instance Detector" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if SQL Server services are running
$instances = Get-Service | Where-Object {
    $_.DisplayName -like "*SQL Server*" -and 
    ($_.Status -eq "Running" -or $_.Status -eq "Stopped")
}

if ($instances.Count -eq 0) {
    Write-Host "❌ No SQL Server instances found on this machine" -ForegroundColor Red
    Write-Host ""
    Write-Host "Solutions:" -ForegroundColor Yellow
    Write-Host "1. Install SQL Server Express (free)" -ForegroundColor White
    Write-Host "2. Or use SQLite in appsettings.json:" -ForegroundColor White
    Write-Host '   "DefaultConnection": "Data Source=chat.db"' -ForegroundColor Gray
    exit
}

Write-Host "Found SQL Server instances:" -ForegroundColor Green
Write-Host ""

$connectionStrings = @()

foreach ($instance in $instances) {
    $status = if ($instance.Status -eq "Running") { "✅ Running" } else { "❌ Stopped" }
    $statusColor = if ($instance.Status -eq "Running") { "Green" } else { "Red" }
    
    Write-Host "  Instance: $($instance.DisplayName)" -ForegroundColor White
    Write-Host "  Status: $status" -ForegroundColor $statusColor
    Write-Host "  Service Name: $($instance.Name)" -ForegroundColor Gray
    
    # Extract instance name
    if ($instance.Name -eq "MSSQLSERVER") {
        $serverName = "localhost"
        $instanceDisplay = "Default Instance"
    }
    else {
        $instanceName = $instance.Name -replace "MSSQL\$", ""
        $serverName = "localhost\$instanceName"
        $instanceDisplay = "Named Instance: $instanceName"
    }
    
    $connectionString = "Server=$serverName;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
    
    Write-Host "  Type: $instanceDisplay" -ForegroundColor Cyan
    Write-Host "  Connection String:" -ForegroundColor Yellow
    Write-Host "    $connectionString" -ForegroundColor Gray
    Write-Host ""
    
    if ($instance.Status -eq "Running") {
        $connectionStrings += $connectionString
    }
}

if ($connectionStrings.Count -eq 0) {
    Write-Host "⚠️  No running SQL Server instances found!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "To start SQL Server:" -ForegroundColor Cyan
    Write-Host "  Start-Service MSSQLSERVER" -ForegroundColor White
    Write-Host "  # or" -ForegroundColor Gray
    Write-Host "  Start-Service MSSQL`$SQLEXPRESS" -ForegroundColor White
    Write-Host ""
    exit
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Recommended Connection String:" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Copy this to appsettings.json:" -ForegroundColor Yellow
Write-Host ""
Write-Host '{' -ForegroundColor Gray
Write-Host '  "ConnectionStrings": {' -ForegroundColor Gray
Write-Host "    `"DefaultConnection`": `"$($connectionStrings[0])`"" -ForegroundColor White
Write-Host '  }' -ForegroundColor Gray
Write-Host '}' -ForegroundColor Gray
Write-Host ""

# Test connection
Write-Host "Testing connection..." -ForegroundColor Cyan
try {
    $testConnection = New-Object System.Data.SqlClient.SqlConnection($connectionStrings[0])
    $testConnection.Open()
    Write-Host "✅ Connection test successful!" -ForegroundColor Green
    $testConnection.Close()
}
catch {
    Write-Host "❌ Connection test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible issues:" -ForegroundColor Yellow
    Write-Host "1. Database 'ChatAppDB' doesn't exist (run SetupDatabase.sql)" -ForegroundColor White
    Write-Host "2. Wrong password for 'sa' account" -ForegroundColor White
    Write-Host "3. SQL Server Authentication not enabled" -ForegroundColor White
    Write-Host "4. Firewall blocking connection" -ForegroundColor White
}

Write-Host ""

