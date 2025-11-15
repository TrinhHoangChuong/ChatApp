# 💬 ChatApp - Ứng dụng Chat Real-time

Ứng dụng chat real-time được xây dựng với ASP.NET Core (C#) cho backend và HTML/CSS/JavaScript cho frontend, với giao diện đẹp giống Discord.

---

## 📋 Mục lục

1. [Tính năng](#-tính-năng)
2. [Cấu trúc Project](#-cấu-trúc-project)
3. [Cài đặt và Chạy ứng dụng](#-cài-đặt-và-chạy-ứng-dụng)
4. [Migration Database](#-migration-database)
5. [Hướng dẫn Test](#-hướng-dẫn-test)
6. [Cấu hình Bảo mật](#-cấu-hình-bảo-mật)
7. [API Endpoints](#-api-endpoints)
8. [Xử lý Lỗi](#-xử-lý-lỗi)
9. [Troubleshooting](#-troubleshooting)

---

## 🎨 Tính năng

- ✅ **Đăng ký / Đăng nhập** với JWT Authentication
- ✅ **Giao diện đẹp** giống Discord/Messenger với dark theme
- ✅ **Chat real-time** với polling (refresh mỗi 2 giây)
- ✅ **Phòng chat (Rooms)** - Tạo và tham gia room để chat nhóm
- ✅ **Chat riêng (Direct Messages)** - Chat 1-1 với người dùng khác
- ✅ **Hiển thị danh sách người dùng** và thành viên room
- ✅ **Lưu trữ tin nhắn** trong database SQLite
- ✅ **Responsive design** - tương thích mobile
- ✅ **Security headers** đầy đủ cho bảo mật

---

## 📁 Cấu trúc Project

```
ChatApp/
├── wwwroot/                  # Frontend (HTML, CSS, JS)
│   ├── index.html            # Trang đăng nhập/đăng ký
│   ├── chat.html             # Trang chat
│   ├── css/
│   │   ├── auth.css          # Styling cho login/register
│   │   └── chat.css          # Styling cho chat interface
│   └── js/
│       ├── auth.js           # Xử lý đăng nhập/đăng ký
│       └── chat.js           # Xử lý chat functionality
├── Controllers/              # API Controllers
│   ├── AuthController.cs    # Authentication endpoints
│   ├── UserController.cs     # User management
│   ├── RoomController.cs     # Room & DM management
│   └── MessageController.cs # Message handling
├── Models/                    # Database Models
│   ├── User.cs               # User model
│   ├── Room.cs               # Room model
│   ├── RoomMember.cs         # Room membership
│   └── Message.cs            # Message model
├── Data/                      # Database Context
│   ├── AppDbContext.cs       # EF Core DbContext
│   └── UserRepository.cs     # User repository
├── Services/                  # Business Logic
│   └── AuthService.cs        # JWT & Password hashing
├── Migrations/                # EF Core Migrations
├── Properties/
│   └── launchSettings.json   # Launch profiles
├── Program.cs                 # Application Entry Point
├── appsettings.json          # Configuration (Development)
├── appsettings.Production.json # Configuration (Production)
├── chat.db                    # SQLite Database
└── START.bat                  # Quick start script
```

---

## 🚀 Cài đặt và Chạy ứng dụng

### Yêu cầu

- .NET 8.0 SDK
- SQLite (tự động qua EF Core)
- Trình duyệt web hiện đại

### Bước 1: Clone/Download project

```bash
cd ChatApp/ChatApp
```

### Bước 2: Cài đặt EF Core Tools (nếu chưa có)

```bash
dotnet tool install --global dotnet-ef
```

Kiểm tra:
```bash
dotnet ef --version
```

### Bước 3: Chạy Migration để tạo Database

```bash
# Tạo migration (nếu chưa có)
dotnet ef migrations add InitDatabase

# Cập nhật database
dotnet ef database update
```

### Bước 4: Chạy Backend

**Cách 1: Sử dụng START.bat (Windows)**
```bash
# Double-click vào file START.bat
# hoặc chạy từ Terminal:
.\START.bat
```

**Cách 2: Chạy trực tiếp**
```bash
dotnet run
```

**Kết quả mong đợi:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5187
      Application started. Press Ctrl+C to shut down.
```

### Bước 5: Truy cập Frontend

Mở trình duyệt và truy cập:
- **Trang đăng nhập/đăng ký:** `http://localhost:5187` hoặc `http://localhost:5187/index.html`
- **Trang chat:** `http://localhost:5187/chat.html` (sau khi đăng nhập)
- **Swagger UI (API Docs):** `http://localhost:5187/swagger`

---

## 🔄 Migration Database

### Tạo Migration mới

Sau khi thêm/sửa Models, tạo migration:

```bash
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Backup Database (Nếu có dữ liệu cũ)

Trước khi migration, nên backup:

```bash
# Windows
copy chat.db chat.db.backup

# Linux/Mac
cp chat.db chat.db.backup
```

### Xóa và tạo lại Database

⚠️ **Cảnh báo:** Xóa database sẽ mất tất cả dữ liệu!

```bash
# Xóa database cũ
del chat.db          # Windows
# hoặc
rm chat.db           # Linux/Mac

# Tạo lại
dotnet ef database update
```

### Cấu trúc Database

Database `chat.db` chứa các bảng:
- `Users` - Thông tin người dùng
- `Rooms` - Phòng chat (public và private DM)
- `RoomMembers` - Thành viên của các room
- `Messages` - Tin nhắn

---

## 🧪 Hướng dẫn Test

### Test Đăng Ký và Đăng Nhập

1. Truy cập `http://localhost:5187`
2. Click tab **"Đăng ký"**
3. Điền thông tin:
   - **Tên đăng nhập:** `testuser1`
   - **Mật khẩu:** `123456` (tối thiểu 6 ký tự)
   - **Xác nhận mật khẩu:** `123456`
4. Click **"Đăng ký"**
5. ✅ Tự động chuyển sang tab đăng nhập
6. Click **"Đăng nhập"** với thông tin vừa đăng ký
7. ✅ Tự động redirect đến `/chat.html`

### Test Tạo Room

1. Click nút **"+ Tạo Room"** ở sidebar trái
2. Điền:
   - **Tên Room:** `General`
   - **Mô tả:** `Room chat chung` (tùy chọn)
3. Click **"Tạo Room"**
4. ✅ Room xuất hiện trong danh sách và tự động được chọn

### Test Chat trong Room

1. Click vào một **Room** trong sidebar trái
2. ✅ Hiển thị thành viên của room ở sidebar giữa
3. Nhập tin nhắn và nhấn **Enter**
4. ✅ Tin nhắn hiển thị ngay

### Test Chat Riêng (Direct Message)

1. Click vào một **user** trong danh sách "TẤT CẢ NGƯỜI DÙNG"
2. ✅ Tự động tạo/đi đến DM room với user đó
3. ✅ DM xuất hiện trong "DIRECT MESSAGES"
4. Nhập tin nhắn và gửi
5. ✅ Chat riêng hoạt động

### Test Tham Gia Room

1. Click vào room mà bạn chưa tham gia
2. ✅ Hiển thị nút **"Tham gia Room"**
3. Click **"Tham gia Room"**
4. ✅ Có thể chat ngay sau khi tham gia

---

## 🔒 Cấu hình Bảo mật

### Development (HTTP - Không có cảnh báo)

Mặc định chạy trên HTTP để tránh cảnh báo certificate:
- URL: `http://localhost:5187`
- Profile: **"ChatApp"** trong `launchSettings.json`

### Production (HTTPS - Đầy đủ bảo mật)

1. **Cấu hình JWT Secret Key:**
   Sửa `appsettings.Production.json`:
   ```json
   {
     "Jwt": {
       "Key": "GENERATE_A_RANDOM_32_CHARACTER_SECRET_KEY_HERE"
     },
     "AllowedOrigins": [
       "https://yourdomain.com"
     ]
   }
   ```

2. **Generate JWT Secret Key:**
   ```powershell
   # PowerShell
   -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 32 | % {[char]$_})
   ```

3. **Set Environment:**
   ```bash
   $env:ASPNETCORE_ENVIRONMENT="Production"
   dotnet run
   ```

### Security Headers đã được cấu hình

- ✅ `X-Content-Type-Options: nosniff`
- ✅ `X-Frame-Options: DENY`
- ✅ `X-XSS-Protection: 1; mode=block`
- ✅ `Referrer-Policy: strict-origin-when-cross-origin`
- ✅ `Content-Security-Policy` (Production)
- ✅ `Strict-Transport-Security` (Production)

### CORS Policy

- **Development:** Chỉ cho phép `localhost:5187` và `localhost:7249`
- **Production:** Chỉ cho phép domain được cấu hình trong `appsettings.json`

### ⚠️ Lưu ý Bảo mật

1. **JWT Secret Key:** KHÔNG BAO GIỜ sử dụng key mặc định trong Production!
2. **CORS:** Production chỉ cho phép domain cụ thể
3. **Database:** SQLite cho development, nên dùng SQL Server/PostgreSQL cho production

---

## 🔌 API Endpoints

### Authentication

- `POST /api/auth/register` - Đăng ký tài khoản mới
  ```json
  {
    "username": "string",
    "passwordHash": "string"
  }
  ```

- `POST /api/auth/login` - Đăng nhập
  ```json
  {
    "username": "string",
    "passwordHash": "string"
  }
  ```
  Response: `{ "token": "JWT_TOKEN" }`

### Users

- `GET /api/user` - Lấy danh sách tất cả người dùng (yêu cầu JWT)
- `GET /api/user/{id}` - Lấy thông tin người dùng theo ID
- `GET /api/user/username/{username}` - Lấy thông tin theo username

### Rooms

- `GET /api/room` - Lấy danh sách rooms (public) và DMs (yêu cầu JWT)
- `POST /api/room` - Tạo room mới (yêu cầu JWT)
  ```json
  {
    "name": "Room Name",
    "description": "Optional description"
  }
  ```
- `POST /api/room/{roomId}/join` - Tham gia room (yêu cầu JWT)
- `POST /api/room/{roomId}/leave` - Rời room (yêu cầu JWT)
- `POST /api/room/dm/{targetUserId}` - Tạo/get DM với user (yêu cầu JWT)
- `GET /api/room/{roomId}/members` - Lấy danh sách thành viên room

### Messages

- `GET /api/message/room/{roomId}` - Lấy tin nhắn trong room (yêu cầu JWT)
- `GET /api/message/dm/{targetUserId}` - Lấy tin nhắn DM (yêu cầu JWT)
- `POST /api/message` - Gửi tin nhắn mới (yêu cầu JWT)
  ```json
  {
    "content": "Nội dung tin nhắn",
    "roomId": 1,              // Cho room message
    "receiverId": 2           // Cho DM message
  }
  ```
- `DELETE /api/message/{id}` - Xóa tin nhắn (yêu cầu JWT)

### JWT Authentication

Tất cả endpoints (trừ register/login) yêu cầu JWT token trong header:
```
Authorization: Bearer {token}
```

Token được lưu trong `localStorage` sau khi đăng nhập thành công.

---

## ❌ Xử lý Lỗi

### Lỗi: "No EF Core tools found"

**Giải pháp:**
```bash
dotnet tool install --global dotnet-ef
```

### Lỗi: "Failed to fetch" hoặc "Network Error"

**Nguyên nhân:** Backend chưa chạy hoặc chạy sai port

**Giải pháp:**
1. Kiểm tra backend đang chạy: `http://localhost:5187`
2. Mở Console trình duyệt (F12) để xem lỗi chi tiết
3. Đảm bảo backend đã start: `dotnet run`

### Lỗi: "Unauthorized" hoặc redirect về login

**Nguyên nhân:** Token hết hạn hoặc không hợp lệ

**Giải pháp:**
1. Xóa localStorage: Mở DevTools (F12) → Console:
   ```javascript
   localStorage.clear()
   ```
2. Refresh trang và đăng nhập lại

### Lỗi: "Username already exists"

**Nguyên nhân:** Username đã tồn tại trong database

**Giải pháp:** Đăng ký với username khác hoặc đăng nhập với username đã có

### Lỗi: "CORS policy blocked"

**Nguyên nhân:** Domain không nằm trong `AllowedOrigins`

**Giải pháp:**
1. Thêm domain vào `appsettings.json` → `AllowedOrigins`
2. Restart server

### Lỗi: "Failed to create room" hoặc "Không thể tạo DM"

**Nguyên nhân:** Token không hợp lệ hoặc lỗi backend

**Giải pháp:**
1. Kiểm tra Console (F12) để xem lỗi chi tiết
2. Kiểm tra Network tab để xem response từ server
3. Đăng nhập lại để lấy token mới

### Lỗi: Database không tìm thấy

**Nguyên nhân:** Chưa chạy migration

**Giải pháp:**
```bash
cd ChatApp\ChatApp
dotnet ef database update
```

---

## 🔧 Troubleshooting

### Kiểm tra Database

```bash
# Sử dụng SQLite command line
sqlite3 chat.db
.tables
SELECT * FROM Users;
SELECT * FROM Rooms;
SELECT * FROM RoomMembers;
SELECT * FROM Messages;
```

### Debug trong Browser

Mở DevTools (F12) → Console:
```javascript
// Kiểm tra token
localStorage.getItem('token')

// Kiểm tra username
localStorage.getItem('username')

// Xóa tất cả
localStorage.clear()
```

### Kiểm tra Backend Logs

Xem terminal nơi chạy `dotnet run` để xem:
- Exception details
- Request logs
- Database errors

### Hard Refresh Browser

Để clear cache và load code mới:
- **Windows/Linux:** `Ctrl + Shift + R` hoặc `Ctrl + F5`
- **Mac:** `Cmd + Shift + R`

---

## 📝 Checklist Trước Khi Deploy Production

- [ ] Đã thay đổi JWT Secret Key trong `appsettings.Production.json`
- [ ] Đã cập nhật `AllowedOrigins` với domain thực tế
- [ ] Đã cấu hình SSL certificate hợp lệ
- [ ] Đã set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Đã kiểm tra security headers
- [ ] Đã backup database
- [ ] Đã test authentication flow
- [ ] Đã test tất cả chức năng: đăng ký, đăng nhập, tạo room, chat, DM

---

## 🔄 Cải thiện trong tương lai

- [ ] Implement SignalR Hub cho real-time chat thực sự (thay thế polling)
- [ ] Upload ảnh/files
- [ ] Emoji picker
- [ ] Typing indicators
- [ ] User status (online/offline/away)
- [ ] Message reactions
- [ ] Edit/Delete messages
- [ ] Search messages
- [ ] Notifications

---

## 📄 License

Dự án đồ án môn học Lập trình C# - Socket Programming

---

## 📞 Liên hệ & Hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. Console trình duyệt (F12)
2. Backend logs (terminal)
3. Network tab trong DevTools
4. Database có dữ liệu chưa

---

**Chúc bạn làm đồ án tốt! 💪**

**Version:** 1.0.0  
**Last Updated:** 2024
