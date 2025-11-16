# 📚 ChatApp - Hướng dẫn đầy đủ

## 🗄️ Database Setup

### **Bước 1: Chạy Script SQL**

1. Mở **SQL Server Management Studio (SSMS)**
2. Connect đến server:
   - **Server name:** `localhost,1433`
   - **Authentication:** SQL Server Authentication
   - **Login:** `sa`
   - **Password:** `123456`
3. Mở file `SetupDatabase.sql` và chạy toàn bộ script (F5)
4. Script sẽ tạo:
   - Database `ChatAppDB`
   - 11 tables: Users, Messages, Guilds, Channels, Memberships, Invitations, Friends, Rooms
   - Tất cả indexes và foreign keys

### **Bước 2: Cấu hình App**

File `appsettings.json` đã được cấu hình sẵn với connection string.

---

## 🏗️ Kiến trúc hệ thống

### **1. Backend (ASP.NET Core + SignalR)**

```
┌─────────────────────────────────────────┐
│         ASP.NET Core Server             │
│  ┌───────────────────────────────────┐  │
│  │   SignalR Hub (ChatHub.cs)        │  │
│  │   - Real-time messaging           │  │
│  │   - Connection management         │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   REST API Controllers            │  │
│  │   - AuthController                │  │
│  │   - MessageController             │  │
│  │   - FriendsController              │  │
│  │   - GuildsController               │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   Database (SQL Server)           │  │
│  │   - ChatAppDB                     │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### **2. Frontend (JavaScript + SignalR Client)**

```
┌─────────────────────────────────────────┐
│         Browser Client                  │
│  ┌───────────────────────────────────┐  │
│  │   SignalR Connection              │  │
│  │   - WebSocket connection          │  │
│  │   - Real-time events              │  │
│  └───────────────────────────────────┘  │
│  ┌───────────────────────────────────┐  │
│  │   REST API Calls                  │  │
│  │   - Authentication                │  │
│  │   - Load data                     │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

---

## 🔄 Cách hoạt động của Real-time Chat

### **1. SignalR Connection Flow**

```
Client                    Server (ChatHub)
  │                           │
  │─── Connect ───────────────>│
  │                           │
  │<── Connection ID ─────────│
  │                           │
  │─── RegisterUser(username)─>│
  │                           │ (Lưu connectionId → username)
  │                           │
  │<── UserList ─────────────│ (Danh sách users online)
```

### **2. Gửi tin nhắn Direct Message (DM)**

```javascript
// Frontend (app.js)
state.connection.invoke("SendDirectMessage", 
    state.username,      // Người gửi
    state.activeDmTarget, // Người nhận
    text                 // Nội dung
);
```

```csharp
// Backend (ChatHub.cs)
public async Task SendDirectMessage(string sender, string recipient, string message)
{
    // 1. Kiểm tra 2 user có là bạn bè không
    var areFriends = await db.Friendships.AnyAsync(...);
    
    // 2. Lưu vào database
    var msg = new Message {
        Sender = username,
        Recipient = recipient,
        Content = message,
        Type = "dm",
        RoomId = null,
        ChannelId = null
    };
    db.Messages.Add(msg);
    await db.SaveChangesAsync();
    
    // 3. Gửi real-time đến cả 2 user
    await Clients.Clients(allTargets)
        .SendAsync("ReceiveDirectMessage", 
            username, recipient, message, timestamp, msg.Id);
}
```

**Luồng hoạt động:**
1. Client A gửi tin nhắn → Server
2. Server lưu vào database
3. Server broadcast đến Client A và Client B (nếu đang online)
4. Cả 2 client nhận tin nhắn real-time qua SignalR

### **3. Tải lịch sử tin nhắn**

```javascript
// Frontend: Khi mở conversation
async function loadDmHistory(username) {
    const params = new URLSearchParams({ 
        userA: state.username, 
        userB: username 
    });
    const data = await fetchJson(
        `${API_BASE}/api/Message/conversation?${params}`
    );
    // Hiển thị lịch sử
    renderMessages(data);
}
```

```csharp
// Backend (MessageController.cs)
[HttpGet("conversation")]
public async Task<IActionResult> GetConversation(
    [FromQuery] string userA, 
    [FromQuery] string userB)
{
    // Lấy tất cả tin nhắn giữa 2 user
    var convo = await _repo.GetConversationAsync(userA, userB, limit: 200);
    return Ok(convo);
}
```

---

## 🧪 Cách Test

### **Test trên cùng 1 máy (Localhost)**

#### **Cách 1: Nhiều tab trình duyệt**

1. **Chạy server:**
   ```bash
   cd ChatApp/ChatApp
   dotnet run
   ```
   Server sẽ chạy tại: `https://localhost:7249`

2. **Mở nhiều tab trình duyệt:**
   - Tab 1: `https://localhost:7249` → Đăng nhập với user `user1`
   - Tab 2: `https://localhost:7249` → Đăng nhập với user `user2`
   - Tab 3: `https://localhost:7249` → Đăng nhập với user `user3`

3. **Test chat:**
   - User1 gửi lời mời kết bạn cho User2
   - User2 chấp nhận
   - User1 và User2 nhắn tin với nhau
   - User3 có thể tạo guild và mời User1, User2 vào

#### **Cách 2: Nhiều cửa sổ trình duyệt**

- Mở **Chrome** → Đăng nhập user1
- Mở **Edge** → Đăng nhập user2
- Mở **Firefox** → Đăng nhập user3
- Test chat giữa các user

#### **Cách 3: Incognito/Private Mode**

- Window 1: Chrome bình thường → user1
- Window 2: Chrome Incognito → user2
- Window 3: Edge Private → user3

### **Test giữa nhiều máy - Cùng mạng LAN**

#### **Bước 1: Tìm IP của máy chạy server**

**Windows:**
```cmd
ipconfig
```
Tìm `IPv4 Address` (ví dụ: `192.168.1.100`)

**Mac/Linux:**
```bash
ifconfig
# hoặc
ip addr
```

**Lưu ý:** IP phải là IP local (192.168.x.x hoặc 10.x.x.x), không phải 127.0.0.1

#### **Bước 2: Cấu hình Firewall trên máy Server**

**Windows:**
1. Mở **Windows Defender Firewall**
2. **Advanced Settings** → **Inbound Rules** → **New Rule**
3. Chọn **Port** → **TCP** → Port `7249` và `5000`
4. Allow connection
5. Áp dụng cho Domain, Private, Public

**Hoặc chạy PowerShell (Admin):**
```powershell
# Cho HTTPS
New-NetFirewallRule -DisplayName "ChatApp HTTPS" -Direction Inbound -LocalPort 7249 -Protocol TCP -Action Allow

# Cho HTTP (nếu cần)
New-NetFirewallRule -DisplayName "ChatApp HTTP" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

**Mac:**
```bash
# Mở System Preferences → Security & Privacy → Firewall → Firewall Options
# Thêm ứng dụng Terminal hoặc .NET
```

**Linux:**
```bash
sudo ufw allow 7249/tcp
sudo ufw allow 5000/tcp
sudo ufw reload
```

#### **Bước 3: Cấu hình Server để lắng nghe trên tất cả interfaces**

**Cách 1: Sửa launchSettings.json (Khuyến nghị)**

Sửa file `Properties/launchSettings.json`:
```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "applicationUrl": "https://0.0.0.0:7249;http://0.0.0.0:5000"
    }
  }
}
```

**Cách 2: Chạy với parameter**
```bash
cd ChatApp/ChatApp
dotnet run --urls "https://0.0.0.0:7249;http://0.0.0.0:5000"
```

**Cách 3: Set environment variable**
```bash
# Windows (PowerShell)
$env:ASPNETCORE_URLS="https://0.0.0.0:7249;http://0.0.0.0:5000"
dotnet run

# Mac/Linux
export ASPNETCORE_URLS="https://0.0.0.0:7249;http://0.0.0.0:5000"
dotnet run
```

#### **Bước 4: Test từ máy khác**

**Máy 1 (Server):**
- IP: `192.168.1.100` (ví dụ)
- Chạy: `dotnet run`
- Server sẽ hiển thị: `Now listening on: https://0.0.0.0:7249`

**Máy 2 (Client - cùng mạng LAN):**
- Mở browser: `https://192.168.1.100:7249`
- ⚠️ Browser sẽ cảnh báo certificate không hợp lệ
- Click **"Advanced"** → **"Proceed to 192.168.1.100 (unsafe)"**
- Đăng nhập và test chat

**Máy 3 (Client - cùng mạng LAN):**
- Mở browser: `https://192.168.1.100:7249`
- Đăng nhập user khác và test chat

**Kiểm tra kết nối:**
```bash
# Từ máy client, ping server để kiểm tra
ping 192.168.1.100

# Hoặc test port
telnet 192.168.1.100 7249
```

---

### **Test giữa nhiều máy - Khác mạng LAN (Internet)**

Khi 2 máy ở khác mạng LAN (ví dụ: máy ở nhà và máy ở công ty), bạn cần expose server ra Internet. Có 3 cách:

#### **Cách 1: Port Forwarding (Router) - Cho mạng riêng**

**Bước 1: Cấu hình Router**

1. Đăng nhập vào Router (thường là `192.168.1.1` hoặc `192.168.0.1`)
2. Tìm **Port Forwarding** hoặc **Virtual Server**
3. Thêm rule:
   - **External Port:** `7249` (hoặc port khác)
   - **Internal IP:** IP của máy server (ví dụ: `192.168.1.100`)
   - **Internal Port:** `7249`
   - **Protocol:** TCP
4. Lưu và apply

**Bước 2: Tìm Public IP của Router**

```bash
# Truy cập từ browser
https://whatismyipaddress.com
# Hoặc
https://ipinfo.io
```

**Bước 3: Chạy Server**

```bash
dotnet run --urls "https://0.0.0.0:7249;http://0.0.0.0:5000"
```

**Bước 4: Test từ máy khác**

- Mở browser: `https://[PUBLIC_IP]:7249`
- Ví dụ: `https://123.45.67.89:7249`

⚠️ **Lưu ý:**
- Cần có Public IP tĩnh (hoặc dùng Dynamic DNS)
- ISP có thể chặn port 7249
- Không an toàn cho production (không có SSL certificate hợp lệ)

---

#### **Cách 2: Ngrok (Khuyến nghị cho test nhanh)**

**Ngrok** tạo tunnel an toàn từ localhost ra Internet.

**Bước 1: Cài đặt Ngrok**

1. Download: https://ngrok.com/download
2. Đăng ký tài khoản miễn phí
3. Lấy authtoken từ dashboard
4. Cấu hình:
   ```bash
   ngrok config add-authtoken YOUR_AUTH_TOKEN
   ```

**Bước 2: Chạy Server**

```bash
cd ChatApp/ChatApp
dotnet run
# Server chạy tại: https://localhost:7249
```

**Bước 3: Chạy Ngrok (Terminal mới)**

```bash
ngrok http 7249
```

Ngrok sẽ hiển thị:
```
Forwarding  https://abc123.ngrok.io -> https://localhost:7249
```

**Bước 4: Test từ máy khác**

- Mở browser: `https://abc123.ngrok.io`
- Đăng nhập và test chat

**Lưu ý:**
- URL ngrok thay đổi mỗi lần chạy (trừ khi dùng plan có trả phí)
- Free plan có giới hạn số lượng connections
- Rất tiện cho test nhanh!

**Cấu hình Ngrok cho HTTPS:**
```bash
ngrok http 7249 --scheme=https
```

---

#### **Cách 3: Deploy lên Cloud (Production)**

**Option A: Azure App Service**

1. Tạo Azure App Service
2. Deploy code lên Azure
3. URL sẽ là: `https://yourapp.azurewebsites.net`
4. Test từ bất kỳ đâu!

**Option B: AWS EC2 / Google Cloud**

1. Tạo VM instance
2. Cài đặt .NET SDK
3. Deploy và chạy app
4. Cấu hình Security Group để mở port 7249
5. Test qua Public IP

**Option C: Railway / Render / Fly.io**

Các platform này hỗ trợ deploy .NET app dễ dàng:
- Railway: https://railway.app
- Render: https://render.com
- Fly.io: https://fly.io

---

### **So sánh các phương pháp:**

| Phương pháp | Độ khó | Chi phí | Tốc độ | Bảo mật | Khuyến nghị |
|------------|--------|---------|--------|---------|-------------|
| **Cùng LAN** | ⭐ Dễ | Miễn phí | ⚡⚡⚡ Nhanh | ✅ Tốt | ✅ Cho test local |
| **Port Forwarding** | ⭐⭐ Trung bình | Miễn phí | ⚡⚡ Trung bình | ⚠️ Cần cẩn thận | Cho mạng riêng |
| **Ngrok** | ⭐ Dễ | Miễn phí (có giới hạn) | ⚡⚡ Trung bình | ✅ Tốt | ✅✅ Cho test nhanh |
| **Cloud Deploy** | ⭐⭐⭐ Khó | Có phí | ⚡⚡⚡ Nhanh | ✅✅ Rất tốt | ✅ Cho production |

---

### **Troubleshooting - Test từ máy khác**

#### **Vấn đề 1: "Connection refused" hoặc không kết nối được**

**Kiểm tra:**
1. Server đang chạy chưa?
2. Firewall đã mở port chưa?
3. Server có lắng nghe trên `0.0.0.0` chưa? (không phải `127.0.0.1`)
4. IP address đúng chưa?

**Test:**
```bash
# Từ máy client
ping [IP_SERVER]
telnet [IP_SERVER] 7249
```

#### **Vấn đề 2: "Certificate error" khi dùng HTTPS**

**Giải pháp:**
- Click "Advanced" → "Proceed" (cho test)
- Hoặc dùng HTTP thay vì HTTPS:
  ```bash
  dotnet run --urls "http://0.0.0.0:5000"
  # Truy cập: http://[IP]:5000
  ```

#### **Vấn đề 3: Ngrok không hoạt động**

**Kiểm tra:**
1. Ngrok đã cấu hình authtoken chưa?
2. Server đang chạy tại port 7249 chưa?
3. Có conflict với firewall không?

**Test:**
```bash
# Kiểm tra ngrok status
ngrok http 7249 --log=stdout
```

#### **Vấn đề 4: Port Forwarding không hoạt động**

**Kiểm tra:**
1. Router có hỗ trợ Port Forwarding không?
2. Public IP có đúng không? (có thể thay đổi)
3. ISP có chặn port không?
4. Server có đang chạy không?

---

### **Best Practices cho Test Production-like**

1. **Dùng Ngrok cho test nhanh:**
   ```bash
   ngrok http 7249
   # Share URL cho team
   ```

2. **Dùng HTTP cho test local (tránh certificate issues):**
   ```bash
   dotnet run --urls "http://0.0.0.0:5000"
   ```

3. **Kiểm tra từ nhiều devices:**
   - Desktop browser
   - Mobile browser
   - Tablet

4. **Test với nhiều users cùng lúc:**
   - Mở nhiều tab
   - Test từ nhiều máy
   - Kiểm tra performance

---

## 🚀 Server hoạt động như thế nào?

### **1. Startup (Program.cs)**

```csharp
// 1. Đăng ký services
builder.Services.AddControllers();
builder.Services.AddSignalR();  // ← SignalR cho real-time
builder.Services.AddDbContext<AppDbContext>();  // ← Database

// 2. Cấu hình routing
app.MapControllers();           // ← REST API
app.MapHub<ChatHub>("/chatHub"); // ← SignalR Hub endpoint
app.MapFallbackToFile("index.html"); // ← Serve frontend
```

### **2. SignalR Hub (ChatHub.cs)**

**Connection Management:**
```csharp
// Dictionary lưu connectionId → username
private static ConcurrentDictionary<string, string> ConnectionUser = new();

// Khi client connect
public async Task RegisterUser(string username) {
    ConnectionUser[Context.ConnectionId] = username;
    await BroadcastUserList();  // Gửi danh sách users online
}
```

**Real-time Messaging:**
```csharp
// Client gọi method này
public async Task SendDirectMessage(string sender, string recipient, string message) {
    // 1. Validate (kiểm tra bạn bè)
    // 2. Lưu vào database
    // 3. Broadcast đến clients
    await Clients.Clients(allTargets)
        .SendAsync("ReceiveDirectMessage", ...);
}
```

**Client nhận event:**
```javascript
// Frontend lắng nghe
state.connection.on("ReceiveDirectMessage", (sender, recipient, content, timestamp, messageId) => {
    // Hiển thị tin nhắn real-time
    appendMessage({ sender, content, timestamp });
});
```

### **3. REST API Controllers**

**Authentication:**
- `POST /api/Auth/login` → Trả về JWT token
- `POST /api/Auth/register` → Tạo user mới

**Messages:**
- `GET /api/Message/conversation?userA=...&userB=...` → Lấy lịch sử chat
- `GET /api/Message?channelId=...` → Lấy tin nhắn channel

**Friends:**
- `GET /api/Friends` → Danh sách bạn bè
- `POST /api/Friends/request` → Gửi lời mời kết bạn
- `POST /api/Friends/requests/{id}/accept` → Chấp nhận lời mời

---

## 📊 Luồng dữ liệu

### **Gửi tin nhắn:**
```
User A (Browser) 
  → SignalR: SendDirectMessage()
  → Server (ChatHub)
  → Database: INSERT INTO Messages
  → SignalR: ReceiveDirectMessage()
  → User A (Browser) ← Nhận lại tin nhắn của chính mình
  → User B (Browser) ← Nhận tin nhắn từ User A
```

### **Tải lịch sử:**
```
User A (Browser)
  → REST API: GET /api/Message/conversation?userA=...&userB=...
  → Server (MessageController)
  → Database: SELECT * FROM Messages WHERE ...
  → Server: Trả về JSON array
  → User A: Hiển thị lịch sử
```

### **Kết bạn:**
```
User A
  → REST API: POST /api/Friends/request { username: "userB" }
  → Server: INSERT INTO FriendRequests
  → User B: Refresh → Thấy lời mời
  → User B: POST /api/Friends/requests/{id}/accept
  → Server: INSERT INTO Friendships (2 records)
  → Cả 2 user: Có thể chat với nhau
```

---

## 🔍 Debug và Troubleshooting

### **Kiểm tra SignalR connection:**

**Browser Console:**
```javascript
// Xem connection state
console.log(state.connection.state); // "Connected" | "Disconnected"

// Test gửi tin nhắn
state.connection.invoke("SendDirectMessage", "user1", "user2", "test");
```

**Server Logs:**
- Xem console output khi chạy `dotnet run`
- Log sẽ hiển thị: Connection, Disconnection, Message sent

### **Kiểm tra Database:**

```sql
-- Xem tất cả users
SELECT * FROM Users;

-- Xem tin nhắn
SELECT * FROM Messages ORDER BY Timestamp DESC;

-- Xem bạn bè
SELECT u1.Username AS User1, u2.Username AS User2
FROM Friendships f
JOIN Users u1 ON f.UserId = u1.Id
JOIN Users u2 ON f.FriendId = u2.Id;
```

### **Common Issues:**

1. **"Cannot send message"**
   - Kiểm tra: 2 user đã kết bạn chưa?
   - Kiểm tra: Database có cột RoomId chưa? (Chạy `AddRoomIdToMessages.sql`)

2. **"Connection failed"**
   - Kiểm tra: Server đang chạy?
   - Kiểm tra: Firewall đã mở port 7249?
   - Kiểm tra: URL đúng chưa? (`https://localhost:7249`)

3. **"History not loading"**
   - Kiểm tra: API endpoint đúng chưa?
   - Kiểm tra: JWT token có trong header?
   - Xem Network tab trong DevTools

---

## 📝 Tóm tắt

- **Backend:** ASP.NET Core + SignalR + SQL Server
- **Frontend:** JavaScript + SignalR Client
- **Real-time:** SignalR WebSocket
- **Database:** SQL Server (ChatAppDB)
- **Port:** 7249 (HTTPS), 5000 (HTTP)

**Test:**
- ✅ Nhiều tab trên cùng máy
- ✅ Nhiều browser trên cùng máy
- ✅ Nhiều máy trong cùng mạng LAN
- ✅ Nhiều client cùng lúc (không giới hạn)

**Server xử lý:**
- Quản lý connections (ConcurrentDictionary)
- Broadcast messages real-time
- Lưu trữ vào database
- Validate permissions (bạn bè, membership)

---

## 🎯 Quick Start

1. **Setup Database:**
   ```sql
   -- Chạy SetupDatabase.sql trong SSMS
   ```

2. **Run Server (Local):**
   ```bash
   cd ChatApp/ChatApp
   dotnet run
   ```

3. **Test Local:**
   - Mở `https://localhost:7249`
   - Đăng ký/đăng nhập
   - Test chat!

4. **Test từ máy khác - Cùng mạng LAN:**
   ```bash
   # Bước 1: Tìm IP của máy server
   ipconfig  # Windows - tìm IPv4 Address (ví dụ: 192.168.1.100)
   
   # Bước 2: Mở Firewall (PowerShell Admin)
   New-NetFirewallRule -DisplayName "ChatApp" -Direction Inbound -LocalPort 7249 -Protocol TCP -Action Allow
   
   # Bước 3: Chạy server với network binding
   dotnet run --urls "https://0.0.0.0:7249;http://0.0.0.0:5000"
   
   # Bước 4: Từ máy khác (cùng mạng LAN)
   # Mở browser: https://192.168.1.100:7249
   # Click "Advanced" → "Proceed" khi có cảnh báo certificate
   ```

5. **Test từ máy khác - Khác mạng (Dùng Ngrok):**
   ```bash
   # Bước 1: Cài đặt Ngrok
   # Download: https://ngrok.com/download
   # Đăng ký và lấy authtoken
   ngrok config add-authtoken YOUR_TOKEN
   
   # Bước 2: Chạy server (Terminal 1)
   dotnet run
   
   # Bước 3: Chạy Ngrok (Terminal 2)
   ngrok http 7249
   # Ngrok sẽ cho URL: https://abc123.ngrok.io
   
   # Bước 4: Share URL ngrok cho người khác test
   # Họ mở: https://abc123.ngrok.io
   ```
