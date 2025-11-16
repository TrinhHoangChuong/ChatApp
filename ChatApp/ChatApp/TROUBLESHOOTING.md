# 🔧 Troubleshooting - Lỗi SQL Server Connection

## ⚠️ Vấn đề: "Cannot connect to SQL Server" trên máy khác

Nếu code chạy OK trên máy bạn nhưng lỗi trên máy bạn khác, đây là checklist:

---

## ✅ Checklist Kiểm tra

### 1. SQL Server Service đang chạy?

**Windows:**
```powershell
# Mở PowerShell (Admin)
Get-Service | Where-Object {$_.Name -like "*SQL*"}

# Hoặc mở Services (services.msc)
# Tìm: SQL Server (MSSQLSERVER) hoặc SQL Server (SQLEXPRESS)
# Status phải là "Running"
```

**Nếu chưa chạy:**
```powershell
# Start SQL Server
Start-Service MSSQLSERVER
# hoặc
Start-Service MSSQL$SQLEXPRESS
```

---

### 2. Kiểm tra Instance Name

Máy bạn có thể dùng instance name khác:

**Kiểm tra:**
```powershell
# PowerShell
Get-Service | Where-Object {$_.DisplayName -like "*SQL Server*"} | Select-Object DisplayName, Name
```

**Các trường hợp thường gặp:**
- `MSSQLSERVER` → Connection string: `Server=localhost;` hoặc `Server=.;`
- `SQLEXPRESS` → Connection string: `Server=localhost\SQLEXPRESS;` hoặc `Server=.\SQLEXPRESS;`
- Named instance khác → `Server=localhost\INSTANCENAME;`

---

### 3. Kiểm tra Port

**Mặc định:**
- SQL Server: Port `1433`
- Named instance: Port động (dynamic port)

**Kiểm tra port:**
```sql
-- Chạy trong SSMS
SELECT 
    local_net_address,
    local_tcp_port
FROM sys.dm_exec_connections
WHERE session_id = @@SPID;
```

**Hoặc:**
```powershell
# PowerShell
Get-NetTCPConnection | Where-Object {$_.LocalPort -eq 1433}
```

---

### 4. Kiểm tra Connection String

**File: `appsettings.json`**

**❌ SAI - Hardcode:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=192.168.1.100,1433;Database=ChatAppDB;..."
  }
}
```

**✅ ĐÚNG - Localhost:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
  }
}
```

**✅ ĐÚNG - Named Instance:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
  }
}
```

**✅ ĐÚNG - Với Port:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
  }
}
```

---

### 5. Test Connection trực tiếp

**Tạo file test: `TestConnection.ps1`**

```powershell
# Test SQL Server Connection
$connectionString = "Server=localhost;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "✅ Connection successful!" -ForegroundColor Green
    $connection.Close()
}
catch {
    Write-Host "❌ Connection failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible issues:" -ForegroundColor Yellow
    Write-Host "1. SQL Server service not running"
    Write-Host "2. Wrong instance name (try: localhost\SQLEXPRESS)"
    Write-Host "3. Wrong password"
    Write-Host "4. Database doesn't exist"
    Write-Host "5. Firewall blocking port 1433"
}
```

**Chạy:**
```powershell
.\TestConnection.ps1
```

---

### 6. Kiểm tra Firewall

**Mở port 1433:**
```powershell
# PowerShell (Admin)
New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow
```

**Kiểm tra rule đã tồn tại:**
```powershell
Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*SQL*"}
```

---

### 7. Kiểm tra SQL Server Authentication

**Trong SSMS:**
1. Connect với Windows Authentication
2. Right-click server → Properties → Security
3. Đảm bảo: **"SQL Server and Windows Authentication mode"** được chọn
4. Restart SQL Server service

**Enable sa account:**
```sql
-- Chạy trong SSMS
ALTER LOGIN sa WITH PASSWORD = '123456';
ALTER LOGIN sa ENABLE;
```

---

### 8. Kiểm tra Database đã tồn tại

**Trong SSMS:**
```sql
-- Kiểm tra database
SELECT name FROM sys.databases WHERE name = 'ChatAppDB';

-- Nếu không có, tạo:
CREATE DATABASE ChatAppDB;
```

**Hoặc chạy lại script:**
```sql
-- Chạy file: Database/SetupDatabase.sql
```

---

## 🔍 Debug Step-by-Step

### Bước 1: Kiểm tra SQL Server đang chạy
```powershell
Get-Service | Where-Object {$_.Name -like "*SQL*"}
```

### Bước 2: Tìm Instance Name
```powershell
Get-Service | Where-Object {$_.DisplayName -like "*SQL Server*"}
```

### Bước 3: Test Connection với SSMS
- Mở SQL Server Management Studio
- Thử connect với các connection string khác nhau:
  - `localhost`
  - `localhost\SQLEXPRESS`
  - `.\SQLEXPRESS`
  - `(local)\SQLEXPRESS`

### Bước 4: Sửa Connection String
Sau khi biết instance name, sửa `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
  }
}
```

### Bước 5: Test lại ứng dụng
```bash
dotnet run
```

---

## 🎯 Giải pháp nhanh nhất

### Option 1: Dùng SQLite (Không cần SQL Server)

**Sửa `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=chat.db"
  }
}
```

**Xóa database cũ (nếu có):**
```bash
# Xóa file chat.db cũ
del chat.db
```

**Chạy lại:**
```bash
dotnet run
```

✅ SQLite sẽ tự động tạo database mới!

---

### Option 2: Tự động detect SQL Server Instance

**Tạo script: `DetectSQLServer.ps1`**

```powershell
# Detect SQL Server instances
Write-Host "Detecting SQL Server instances..." -ForegroundColor Cyan

$instances = Get-Service | Where-Object {
    $_.DisplayName -like "*SQL Server*" -and 
    $_.Status -eq "Running"
}

if ($instances.Count -eq 0) {
    Write-Host "❌ No SQL Server instances found or not running" -ForegroundColor Red
    Write-Host "Please start SQL Server service or use SQLite" -ForegroundColor Yellow
    exit
}

Write-Host "`nFound SQL Server instances:" -ForegroundColor Green
foreach ($instance in $instances) {
    Write-Host "  - $($instance.DisplayName)" -ForegroundColor White
    
    # Extract instance name
    if ($instance.Name -eq "MSSQLSERVER") {
        $serverName = "localhost"
    }
    else {
        $instanceName = $instance.Name -replace "MSSQL\$", ""
        $serverName = "localhost\$instanceName"
    }
    
    Write-Host "    Connection string: Server=$serverName;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;" -ForegroundColor Gray
}

Write-Host "`nCopy connection string above to appsettings.json" -ForegroundColor Yellow
```

**Chạy:**
```powershell
.\DetectSQLServer.ps1
```

---

## 📝 Template Connection Strings

### Default Instance (MSSQLSERVER)
```json
"DefaultConnection": "Server=localhost;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
```

### Named Instance (SQLEXPRESS)
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
```

### Với Port cụ thể
```json
"DefaultConnection": "Server=localhost,1433;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
```

### Windows Authentication (nếu có quyền)
```json
"DefaultConnection": "Server=localhost;Database=ChatAppDB;Integrated Security=True;TrustServerCertificate=True;"
```

---

## 🚨 Lỗi thường gặp và Fix

### Lỗi: "A network-related or instance-specific error"
- ✅ Kiểm tra SQL Server service đang chạy
- ✅ Kiểm tra instance name đúng
- ✅ Kiểm tra firewall

### Lỗi: "Login failed for user 'sa'"
- ✅ Enable Mixed Mode Authentication
- ✅ Enable sa account
- ✅ Kiểm tra password đúng

### Lỗi: "Cannot open database 'ChatAppDB'"
- ✅ Database chưa được tạo
- ✅ Chạy script `Database/SetupDatabase.sql`

### Lỗi: "Server was not found or was not accessible"
- ✅ SQL Server service chưa chạy
- ✅ Instance name sai
- ✅ Port bị chặn bởi firewall

---

## 💡 Khuyến nghị

**Cho Development:**
- Dùng SQLite (dễ setup, không cần cấu hình)
- Connection: `Data Source=chat.db`

**Cho Production:**
- Dùng SQL Server (ổn định, hiệu năng tốt)
- Đảm bảo connection string đúng cho từng máy

---

## 📞 Cần giúp thêm?

1. Chạy `DetectSQLServer.ps1` để tự động detect
2. Test connection với SSMS
3. Kiểm tra logs trong console khi chạy `dotnet run`
4. Xem error message chi tiết trong browser console (F12)

