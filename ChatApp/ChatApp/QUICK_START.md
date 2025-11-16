# ⚡ Quick Start - ChatApp

## 🚀 Chạy nhanh (Dùng SQLite - Không cần setup gì)

1. **Clone/Pull code:**
   ```bash
   git clone https://github.com/TrinhHoangChuong/ChatApp.git
   cd ChatApp/ChatApp
   ```

2. **Chạy ứng dụng:**
   ```bash
   dotnet run
   ```

3. **Mở browser:**
   ```
   https://localhost:7249
   ```

✅ **Xong!** Database SQLite sẽ tự động tạo file `chat.db`

---

## ⚠️ Nếu gặp lỗi "SQL Server connection refused"

### Giải pháp nhanh: Dùng SQLite

1. **Kiểm tra file `appsettings.Development.json`:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=chat.db"
     }
   }
   ```

2. **Nếu file không có ConnectionStrings, thêm vào như trên**

3. **Chạy lại:**
   ```bash
   dotnet run
   ```

### Hoặc sửa appsettings.json

Mở `appsettings.json` và đảm bảo:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=chat.db"
  }
}
```

---

## 📋 Nếu muốn dùng SQL Server

Xem file `SETUP_GUIDE.md` để biết cách setup SQL Server chi tiết.

**Tóm tắt:**
1. Cài SQL Server Express
2. Chạy script `Database/SetupDatabase.sql`
3. Copy `appsettings.SQLServer.json` → `appsettings.json`
4. Sửa connection string nếu cần

---

## 🔍 Kiểm tra lỗi

Khi chạy `dotnet run`, xem console output:
- ✅ `[Database] Configured for SQLite` → OK
- ✅ `[Database] SQLite database created/verified successfully` → OK
- ❌ `[Database] Error connecting to SQL Server` → Xem SETUP_GUIDE.md

---

**Xem thêm:** `SETUP_GUIDE.md` để biết chi tiết troubleshooting

