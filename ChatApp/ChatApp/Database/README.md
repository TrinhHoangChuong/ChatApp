# Database Setup Instructions

## 🚀 Cách sử dụng SQL Script

### **Bước 1: Mở SSMS và Connect**

1. Mở **SQL Server Management Studio (SSMS)**
2. Connect đến server:
   - **Server name:** `localhost,1433` hoặc `localhost\SQLEXPRESS`
   - **Authentication:** SQL Server Authentication
   - **Login:** `sa`
   - **Password:** `123456`

### **Bước 2: Chạy Script Setup**

1. Mở file `SetupDatabase.sql`
2. Chạy toàn bộ script (F5 hoặc Execute)
3. Script sẽ tự động:
   - Tạo database `ChatAppDB` (nếu chưa có)
   - Xóa các tables cũ (nếu có)
   - Tạo tất cả 7 tables mới
   - Tạo indexes
   - Hiển thị báo cáo kết quả

### **Bước 3: Cấu hình App**

File `appsettings.json` đã được cấu hình với:
- Server: `localhost,1433`
- Database: `ChatAppDB`
- Username: `sa`
- Password: `123456`

### **Bước 4: Chạy Application**

```bash
cd ChatApp/ChatApp
dotnet restore
dotnet ef database update
dotnet run
```

---

## 📋 Danh sách Tables

1. **Users** - Thông tin người dùng
2. **Messages** - Tin nhắn (channel và DM)
3. **Guilds** - Máy chủ
4. **Channels** - Kênh trong máy chủ
5. **GuildMemberships** - Thành viên máy chủ
6. **FriendRequests** - Lời mời kết bạn
7. **Friendships** - Quan hệ bạn bè

---

## ✅ Kiểm tra

### **Cách 1: Dùng Script**
Chạy file `VerifyTables.sql` để kiểm tra tự động:
- Sẽ hiển thị tables nào đã có và tables nào còn thiếu
- Tổng số tables: 7 / 7

### **Cách 2: Kiểm tra thủ công**
Trong SSMS:
1. Expand `Databases` → `ChatAppDB` → `Tables`
2. Bạn sẽ thấy 7 tables:
   - ✅ Users
   - ✅ Messages
   - ✅ Guilds
   - ✅ Channels
   - ✅ GuildMemberships
   - ✅ FriendRequests
   - ✅ Friendships
3. Có thể query data: `SELECT * FROM Users;`

### **Nếu cần kiểm tra lại:**
Chạy file `VerifyTables.sql` để kiểm tra xem tất cả tables đã được tạo chưa

---

## 🔄 Migration từ SQLite

Nếu bạn đã có data trong SQLite và muốn migrate:
1. Export data từ SQLite
2. Import vào SQL Server
3. Hoặc để EF Core tự migrate khi chạy `dotnet ef database update`

