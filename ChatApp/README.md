# ChatApp - Discord-like Chat Application

Ứng dụng chat realtime giống Discord, xây dựng trên **ASP.NET Core 8** với **SignalR**, **EF Core/SQLite** ở backend và frontend thuần **HTML/CSS/JavaScript** (đặt trong `ChatApp/ChatApp/wwwroot`).

---

## 📋 Tổng quan tính năng đã triển khai

### ✅ **Đã hoàn thiện và hoạt động:**

1. **🔐 Xác thực người dùng**
   - Đăng ký tài khoản mới
   - Đăng nhập với JWT token
   - Quản lý phiên đăng nhập (localStorage)

2. **💬 Chat realtime**
   - ✅ Chat trong kênh (channel) của máy chủ
   - ✅ Chat riêng tư (Direct Message - DM) giữa 2 người dùng
   - ✅ Gửi sticker/ảnh qua SignalR
   - ✅ Typing indicator (hiển thị khi người khác đang gõ)
   - ✅ Online/Offline presence (hiển thị trạng thái online)
   - ✅ Lịch sử tin nhắn (load tin nhắn cũ khi vào kênh/DM)

3. **👥 Quản lý bạn bè**
   - ✅ Tìm kiếm người dùng theo username
   - ✅ Gửi lời mời kết bạn
   - ✅ Chấp nhận/Từ chối lời mời kết bạn
   - ✅ Xem danh sách bạn bè
   - ✅ Gỡ bạn bè

4. **🏰 Quản lý máy chủ (Guild/Server)**
   - ✅ Tạo máy chủ mới
   - ✅ Tạo kênh (channel) trong máy chủ
   - ✅ Tham gia máy chủ
   - ✅ Xem danh sách máy chủ đã tham gia
   - ✅ Xem danh sách kênh trong máy chủ

5. **🔍 Tìm kiếm người dùng**
   - ✅ Tìm kiếm tất cả người dùng trong hệ thống (`GET /api/User`)
   - ✅ Lọc người dùng theo username trong giao diện
   - ✅ Hiển thị danh sách người dùng online

6. **📁 Upload file**
   - ✅ Upload file/ảnh lên server
   - ✅ Gửi file/ảnh trong chat

---

## 🏗️ Kiến trúc Backend

### **Công nghệ chính:**
- ASP.NET Core 8 Web API
- SignalR Hub (realtime communication)
- Entity Framework Core + SQLite
- JWT + BCrypt (authentication & password hashing)

### **Các Controller và chức năng:**

#### 1. **`AuthController`** - Xác thực
- `POST /api/Auth/register` - Đăng ký tài khoản mới
- `POST /api/Auth/login` - Đăng nhập, nhận JWT token

#### 2. **`UserController`** - Quản lý người dùng
- `GET /api/User` - Lấy danh sách tất cả người dùng (✅ **Dùng để tìm user**)
- `GET /api/User/{id}` - Lấy thông tin user theo ID
- `GET /api/User/username/{username}` - Tìm user theo username
- `POST /api/User` - Tạo user mới
- `PUT /api/User/{id}` - Cập nhật thông tin user
- `DELETE /api/User/{id}` - Xóa user

#### 3. **`MessageController`** - Quản lý tin nhắn
- `GET /api/Message?channelId={id}&limit={n}` - Lấy tin nhắn trong kênh
- `GET /api/Message?limit={n}` - Lấy tin nhắn công khai gần đây
- `GET /api/Message/conversation?userA={user}&userB={user}` - Lấy lịch sử chat riêng

#### 4. **`GuildsController`** - Quản lý máy chủ và kênh
- `GET /api/Guilds` - Lấy danh sách máy chủ của user hiện tại
- `POST /api/Guilds` - Tạo máy chủ mới
- `GET /api/Guilds/{id}` - Lấy thông tin chi tiết máy chủ
- `POST /api/Guilds/{id}/channels` - Tạo kênh mới trong máy chủ
- `POST /api/Guilds/{id}/join` - Tham gia máy chủ

#### 5. **`FriendsController`** - Quản lý bạn bè
- `GET /api/Friends` - Lấy danh sách bạn bè
- `GET /api/Friends/requests` - Lấy danh sách lời mời kết bạn (nhận/gửi)
- `POST /api/Friends/request` - Gửi lời mời kết bạn
- `POST /api/Friends/requests/{id}/accept` - Chấp nhận lời mời
- `POST /api/Friends/requests/{id}/reject` - Từ chối lời mời
- `DELETE /api/Friends/{friendId}` - Gỡ bạn bè

#### 6. **`FileController`** - Upload file
- `POST /api/File/upload` - Upload file/ảnh, trả về URL công khai

### **SignalR Hub (`ChatHub`) - Realtime Communication**

**Endpoint:** `/chatHub`

**Các method client có thể gọi:**
- `RegisterUser(username)` - Đăng ký user khi kết nối
- `JoinChannel(channelId)` - Tham gia kênh để nhận tin nhắn
- `LeaveChannel(channelId)` - Rời kênh
- `SendChannelMessage(channelId, message)` - Gửi tin nhắn trong kênh
- `SendChannelSticker(channelId, stickerUrl)` - Gửi sticker trong kênh
- `SendDirectMessage(sender, recipient, message)` - Gửi tin nhắn riêng
- `OpenDirectChannel(requester, peer)` - Mở kênh chat riêng với user khác
- `Typing(username)` - Báo hiệu đang gõ
- `StopTyping(username)` - Báo hiệu dừng gõ

**Các event server gửi đến client:**
- `ReceiveChannelMessage(channelId, sender, message, timestamp)` - Nhận tin nhắn kênh
- `ReceiveChannelSticker(channelId, sender, stickerUrl, timestamp)` - Nhận sticker kênh
- `ReceiveDirectMessage(sender, recipient, message, timestamp)` - Nhận tin nhắn riêng
- `UserList(users[])` - Danh sách user online
- `UserConnected(username)` - User vừa online
- `UserDisconnected(username)` - User vừa offline
- `UserTyping(username)` - User đang gõ
- `UserStopTyping(username)` - User dừng gõ
- `DirectHistory(peer, messages[])` - Lịch sử chat riêng

### 🔌 Luồng realtime chi tiết (C# backend ↔ frontend JS)

1. **Kết nối & đăng ký**
   - Frontend tạo `HubConnection` trong `wwwroot/app.js`, gọi `RegisterUser` ngay sau khi `connection.start()` thành công.
   - Backend giữ `ConnectionId ↔ Username` trong `ConnectionUser` dictionary để broadcast trạng thái online/offline.

2. **Chat trực tiếp (DM)**
   - Khi người dùng mở DM, frontend gọi `OpenDirectChannel(user, peer)` => server add connection vào group `dm:{user}:{peer}` và trả lịch sử qua sự kiện `DirectHistory`.
   - Gửi tin nhắn: client invoke `SendDirectMessage`.

```csharp
// ChatApp/Hubs/ChatHub.cs
public async Task SendDirectMessage(string sender, string recipient, string message)
{
    var username = GetUsername() ?? sender;
    ... // kiểm tra bạn bè & lưu DB
    var targets = GetConnections(recipient)
        .Concat(GetConnections(username))
        .Distinct()
        .ToList();
    if (targets.Count > 0)
    {
        await Clients.Clients(targets)
            .SendAsync("ReceiveDirectMessage", username, recipient, message, payloadTime);
    }
}

public async Task SendDirectAttachment(string sender, string recipient, string mediaUrl, string? fileName = null)
{
    ... // validate, lưu Message với MediaUrl
    var targets = GetConnections(recipient)
        .Concat(GetConnections(username))
        .Distinct()
        .ToList();
    if (targets.Count > 0)
    {
        await Clients.Clients(targets)
            .SendAsync("ReceiveDirectAttachment", username, recipient, mediaUrl, fileName, payloadTime);
    }
}
```

- Frontend lắng nghe:

```javascript
// ChatApp/wwwroot/app.js
state.connection.on("ReceiveDirectMessage", (sender, recipient, content, timestamp) => {
    const peer = sender === state.username ? recipient : sender;
    const payload = { sender, recipient, content, timestamp, type: "dm" };
    ensureDmThread(peer).push(payload);
    if (state.activeDmTarget === peer) appendMessage(payload);
});

state.connection.on("ReceiveDirectAttachment", (sender, recipient, mediaUrl, fileName, timestamp) => {
    const peer = sender === state.username ? recipient : sender;
    const payload = { sender, recipient, mediaUrl, content: fileName, timestamp, type: "attachment" };
    ensureDmThread(peer).push(payload);
    if (state.activeDmTarget === peer) appendMessage(payload);
});
```

3. **Upload file trong DM**
   - Form upload (`#file-input`) gửi file lên `/api/File/upload`, lấy `fileUrl`.
   - Nếu đang ở DM, client gọi `SendDirectAttachment` để lưu & broadcast.
   - Trong `appendMessage`, nếu `mediaUrl` là ảnh (`.png/.jpg/...`) thì render `<img>` inline; ngược lại hiển thị link tải.

4. **Chat kênh**
   - Client gọi `JoinChannel(channelId)` khi chọn kênh.
   - `SendChannelMessage`/`SendChannelSticker` lưu DB và `Clients.Group(channel:channelId)` broadcast tới mọi người trong kênh.

### **Database Models:**

- **`User`** - Thông tin người dùng
- **`Message`** - Tin nhắn (hỗ trợ `Type`: "text", "sticker", "dm"; `ChannelId` cho kênh; `Recipient` cho DM)
- **`Guild`** - Máy chủ
- **`Channel`** - Kênh trong máy chủ
- **`GuildMembership`** - Thành viên của máy chủ
- **`FriendRequest`** - Lời mời kết bạn
- **`Friendship`** - Quan hệ bạn bè

### **Cấu hình đặc biệt (`Program.cs`):**
- Tự động migrate database khi khởi động
- Tự động thêm cột `Recipient` và `ChannelId` vào bảng `Messages` nếu chưa có
- Đăng ký tất cả services/repositories vào Dependency Injection
- Cấu hình static files và SPA fallback
- Swagger UI trong môi trường Development

---

## 🎨 Frontend (`ChatApp/ChatApp/wwwroot`)

### **Cấu trúc file:**
- **`index.html`** - Giao diện Discord-like với layout 3 cột:
  - **Cột trái**: Danh sách máy chủ (server rail)
  - **Cột giữa**: Danh sách kênh/DM và khung chat chính
  - **Cột phải**: Danh sách thành viên online, hoạt động, tìm kiếm user
- **`styles.css`** - Giao diện nền tối, glassmorphic, responsive
- **`app.js`** - Logic xử lý kết nối REST API + SignalR, quản lý state

### **Luồng hoạt động chi tiết:**

1. **Khởi động:**
   - Kiểm tra session trong localStorage
   - Nếu có token → tự động đăng nhập
   - Kết nối SignalR hub
   - Load danh sách máy chủ, bạn bè, lời mời kết bạn

2. **Chat trong kênh:**
   - User chọn máy chủ → hiển thị danh sách kênh
   - User chọn kênh → gọi `GET /api/Message?channelId=...` để load lịch sử
   - Gọi `connection.invoke("JoinChannel", channelId)` để tham gia nhóm SignalR
   - Gửi tin nhắn qua `SendChannelMessage` hoặc `SendChannelSticker`
   - Nhận tin nhắn realtime qua `ReceiveChannelMessage`/`ReceiveChannelSticker`

3. **Chat riêng (DM):**
   - User chọn bạn bè từ danh sách hoặc tìm user → mở DM
   - Gọi `OpenDirectChannel` để load lịch sử chat
   - Gửi tin nhắn qua `SendDirectMessage`
   - Nhận tin nhắn realtime qua `ReceiveDirectMessage`
   - Lưu lịch sử DM vào localStorage

4. **Tìm kiếm user:**
   - Frontend gọi `GET /api/User` để lấy danh sách tất cả user
   - Hiển thị trong phần "Directory" (cột phải)
   - User có thể tìm kiếm/filter theo username trong ô search
   - Click vào user để xem thông tin hoặc gửi lời mời kết bạn

5. **Quản lý bạn bè:**
   - Mở Friend Center modal
   - Nhập username → gửi lời mời qua `POST /api/Friends/request`
   - Xem danh sách lời mời nhận được → chấp nhận/từ chối
   - Xem danh sách bạn bè → click để mở DM hoặc gỡ bạn

6. **Quản lý máy chủ:**
   - Tạo máy chủ mới qua modal → `POST /api/Guilds`
   - Tạo kênh mới trong máy chủ → `POST /api/Guilds/{id}/channels`
   - Tham gia máy chủ → `POST /api/Guilds/{id}/join`

---

## 🚀 Hướng dẫn chạy ứng dụng

### **Yêu cầu:**
- .NET SDK 8.0 trở lên
- Trình duyệt web hiện đại (Chrome, Edge, Firefox...)

### **Các bước:**

1. **Mở terminal và di chuyển vào thư mục dự án:**
   ```bash
   cd ChatApp
   ```

2. **Chạy ứng dụng:**
   ```bash
   dotnet run --project ChatApp/ChatApp
   ```
   
   **Lưu ý:** Lần đầu chạy, server sẽ tự động:
   - Tạo file database SQLite (`chat.db`)
   - Tạo các bảng cần thiết
   - Thêm các cột `Recipient` và `ChannelId` vào bảng `Messages` nếu chưa có

3. **Truy cập ứng dụng:**
   - Frontend: `https://localhost:5001` hoặc `http://localhost:5000`
   - Swagger API docs: `https://localhost:5001/swagger`

4. **Sử dụng:**
   - Đăng ký tài khoản mới hoặc đăng nhập
   - Tạo máy chủ và kênh để bắt đầu chat nhóm
   - Tìm kiếm user và gửi lời mời kết bạn để chat riêng
   - Upload file/ảnh và gửi trong chat

---

## 📝 Ghi chú

- **Database:** SQLite file (`chat.db`) được tạo tự động trong thư mục `ChatApp/ChatApp/`
- **JWT Secret:** Hiện tại dùng secret mặc định, nên thay đổi trước khi deploy production
- **CORS:** Chưa cấu hình CORS, phù hợp cho development local
- **HTTPS:** Có thể cần cấu hình reverse proxy (nginx, IIS) cho production
- **Rate Limiting:** Chưa có, nên thêm để tránh abuse

---

## 🎯 Tính năng đã hoàn thiện

| Tính năng | Trạng thái | Mô tả |
|-----------|------------|-------|
| Đăng ký/Đăng nhập | ✅ Hoàn thiện | JWT authentication, session management |
| Chat trong kênh | ✅ Hoàn thiện | Realtime chat với SignalR, lịch sử tin nhắn |
| Chat riêng (DM) | ✅ Hoàn thiện | Chat 1-1 giữa 2 user, lưu lịch sử |
| Gửi sticker/ảnh | ✅ Hoàn thiện | Upload file và gửi trong chat |
| Typing indicator | ✅ Hoàn thiện | Hiển thị khi người khác đang gõ |
| Online/Offline status | ✅ Hoàn thiện | Hiển thị trạng thái user |
| Tìm kiếm user | ✅ Hoàn thiện | Tìm tất cả user, filter theo username |
| Kết bạn | ✅ Hoàn thiện | Gửi/chấp nhận/từ chối lời mời, quản lý bạn bè |
| Tạo máy chủ | ✅ Hoàn thiện | Tạo server, tạo kênh, tham gia server |
| Lịch sử tin nhắn | ✅ Hoàn thiện | Load tin nhắn cũ khi vào kênh/DM |

---

**Ứng dụng đã sẵn sàng để sử dụng với đầy đủ tính năng chat như Discord!** 🎉

