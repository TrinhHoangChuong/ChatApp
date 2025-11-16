# 🚀 Hướng dẫn Setup ChatApp cho máy mới

## ⚠️ Vấn đề thường gặp: "SQL Server connection refused"

Nếu bạn gặp lỗi khi đăng ký/đăng nhập, đây là cách fix:

---

## 📋 Cách 1: Dùng SQLite (Khuyến nghị - Dễ nhất)

SQLite không cần cài đặt gì, tự động tạo database file.

### Bước 1: Kiểm tra appsettings.Development.json

File này phải có:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=chat.db"
  }
}
```

### Bước 2: Chạy ứng dụng

```bash
cd ChatApp/ChatApp
dotnet run
```

✅ **Xong!** Database sẽ tự động tạo file `chat.db` trong thư mục `ChatApp/ChatApp/`

---

## 📋 Cách 2: Dùng SQL Server (Nếu cần)

### Bước 1: Cài đặt SQL Server

1. **Download SQL Server Express** (miễn phí):
   - https://www.microsoft.com/sql-server/sql-server-downloads
   - Chọn "Express" edition

2. **Cài đặt:**
   - Chọn "Mixed Mode Authentication"
   - Đặt password cho `sa` account (ví dụ: `123456`)
   - Ghi nhớ port (mặc định: `1433`)

3. **Kiểm tra SQL Server đang chạy:**
   - Mở **Services** (services.msc)
   - Tìm **SQL Server (MSSQLSERVER)** hoặc **SQL Server (SQLEXPRESS)**
   - Đảm bảo status là **Running**

### Bước 2: Tạo Database

1. Mở **SQL Server Management Studio (SSMS)**
2. Connect với:
   - Server: `localhost` hoặc `localhost\SQLEXPRESS`
   - Authentication: SQL Server Authentication
   - Login: `sa`
   - Password: `123456` (hoặc password bạn đã đặt)

3. Chạy script: `Database/SetupDatabase.sql`

### Bước 3: Cấu hình Connection String

**Option A: Sửa appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
  }
}
```

**Option B: Copy file cấu hình SQL Server**
```bash
# Copy file mẫu
copy appsettings.SQLServer.json appsettings.json
```

**Option C: Dùng Environment Variable**
```bash
# Windows PowerShell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
dotnet run
```

### Bước 4: Kiểm tra kết nối

```bash
dotnet run
```

Nếu thấy lỗi connection, xem phần Troubleshooting bên dưới.

---

## 🔧 Troubleshooting SQL Server

### Lỗi 1: "Server was not found or was not accessible"

**Nguyên nhân:** SQL Server chưa chạy hoặc port sai

**Giải pháp:**
1. Kiểm tra SQL Server đang chạy:
   ```cmd
   # Mở Services (services.msc)
   # Tìm "SQL Server (MSSQLSERVER)" → Start nếu chưa chạy
   ```

2. Kiểm tra port:
   ```cmd
   # Mở SQL Server Configuration Manager
   # SQL Server Network Configuration → Protocols for MSSQLSERVER
   # Đảm bảo TCP/IP đã Enable
   # Xem port trong TCP/IP Properties → IP Addresses
   ```

3. Kiểm tra SQL Server Browser đang chạy (nếu dùng named instance)

### Lỗi 2: "Login failed for user 'sa'"

**Nguyên nhân:** Password sai hoặc account bị disable

**Giải pháp:**
1. Đăng nhập SSMS với Windows Authentication
2. Right-click server → Properties → Security
3. Chọn "SQL Server and Windows Authentication mode"
4. Restart SQL Server service
5. Reset password cho `sa`:
   ```sql
   ALTER LOGIN sa WITH PASSWORD = '123456';
   ALTER LOGIN sa ENABLE;
   ```

### Lỗi 3: "A network-related or instance-specific error"

**Nguyên nhân:** Firewall chặn hoặc SQL Server không cho phép remote connection

**Giải pháp:**
1. **Mở Firewall:**
   ```powershell
   # PowerShell (Admin)
   New-NetFirewallRule -DisplayName "SQL Server" -Direction Inbound -LocalPort 1433 -Protocol TCP -Action Allow
   ```

2. **Enable TCP/IP trong SQL Server:**
   - SQL Server Configuration Manager
   - SQL Server Network Configuration → Protocols
   - Enable TCP/IP
   - Restart SQL Server service

### Lỗi 4: Instance name khác (SQLEXPRESS)

Nếu cài SQL Server Express, instance name là `SQLEXPRESS`:

**Connection String:**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS,1433;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
```

Hoặc:
```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;"
```

---

## 🧪 Test Connection String

Tạo file test: `TestConnection.cs` (tạm thời)

```csharp
using Microsoft.Data.SqlClient;

var connectionString = "Server=localhost,1433;Database=ChatAppDB;User Id=sa;Password=123456;TrustServerCertificate=True;";
try
{
    using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("✅ Connection successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Connection failed: {ex.Message}");
}
```

---

## 📝 Checklist Setup

- [ ] SQL Server đã cài đặt (nếu dùng SQL Server)
- [ ] SQL Server service đang chạy
- [ ] Database đã được tạo (chạy SetupDatabase.sql)
- [ ] Connection string đúng trong appsettings.json
- [ ] Firewall đã mở port 1433 (nếu dùng SQL Server)
- [ ] TCP/IP đã enable trong SQL Server Configuration Manager
- [ ] Password `sa` đúng
- [ ] Mixed Mode Authentication đã enable

---

## 🎯 Khuyến nghị

**Cho Development:** Dùng SQLite (dễ setup, không cần cấu hình)
- File: `appsettings.Development.json`
- Connection: `Data Source=chat.db`

**Cho Production:** Dùng SQL Server (ổn định, hiệu năng tốt)
- File: `appsettings.Production.json` hoặc `appsettings.json`
- Connection: SQL Server connection string

---

## 💡 Quick Fix

Nếu không muốn setup SQL Server, đơn giản nhất:

1. **Xóa hoặc đổi tên** `appsettings.json`
2. **Đảm bảo** `appsettings.Development.json` có:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=chat.db"
     }
   }
   ```
3. **Chạy:** `dotnet run`

Ứng dụng sẽ tự động dùng SQLite! ✅

