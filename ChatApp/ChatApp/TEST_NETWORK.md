# 🌐 Hướng dẫn Test ChatApp giữa 2 máy khác mạng

## 📋 Mục lục

1. [Test cùng mạng LAN](#1-test-cùng-mạng-lan)
2. [Test khác mạng - Dùng Ngrok](#2-test-khác-mạng---dùng-ngrok)
3. [Troubleshooting](#3-troubleshooting)

---

## 1. Test cùng mạng LAN

### Bước 1: Tìm IP của máy Server

**Windows:**
```powershell
ipconfig
# Tìm "IPv4 Address" (ví dụ: 192.168.1.100)
```

**Mac/Linux:**
```bash
ifconfig
# hoặc
ip addr
```

### Bước 2: Mở Firewall (Máy Server)

**Windows PowerShell (Admin):**
```powershell
# Mở port HTTPS (7249)
New-NetFirewallRule -DisplayName "ChatApp HTTPS" -Direction Inbound -LocalPort 7249 -Protocol TCP -Action Allow

# Mở port HTTP (5187) - nếu cần
New-NetFirewallRule -DisplayName "ChatApp HTTP" -Direction Inbound -LocalPort 5187 -Protocol TCP -Action Allow
```

### Bước 3: Chạy Server với Network Binding

**Cách 1: Sửa launchSettings.json**
```json
{
  "profiles": {
    "Network": {
      "commandName": "Project",
      "applicationUrl": "https://0.0.0.0:7249;http://0.0.0.0:5187",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Cách 2: Chạy với command line**
```bash
cd ChatApp/ChatApp
dotnet run --urls "https://0.0.0.0:7249;http://0.0.0.0:5187"
```

### Bước 4: Test từ máy khác (cùng mạng LAN)

1. **Mở browser trên máy Client**
2. **Truy cập:** `https://192.168.1.100:7249` (thay bằng IP của máy Server)
3. **Click "Advanced"** khi có cảnh báo certificate
4. **Click "Proceed to 192.168.1.100 (unsafe)"**

✅ **Xong!** Bạn có thể đăng ký và chat!

---

## 2. Test khác mạng - Dùng Ngrok

### Bước 1: Cài đặt Ngrok

**Download:**
- https://ngrok.com/download
- Hoặc dùng Chocolatey: `choco install ngrok`

**Đăng ký và lấy authtoken:**
1. Đăng ký tại: https://dashboard.ngrok.com/signup
2. Vào: https://dashboard.ngrok.com/get-started/your-authtoken
3. Copy authtoken

**Cấu hình:**
```bash
ngrok config add-authtoken YOUR_AUTHTOKEN
```

### Bước 2: Chạy Server (Máy Server)

**Terminal 1 - Chạy ChatApp:**
```bash
cd ChatApp/ChatApp
dotnet run
```

**Kết quả mong đợi:**
```
Now listening on: https://localhost:7249
Now listening on: http://localhost:5187
```

### Bước 3: Chạy Ngrok (Máy Server)

**Terminal 2 - Chạy Ngrok:**
```bash
# Forward HTTPS port 7249
ngrok http 7249
```

**Kết quả:**
```
Forwarding   https://abc123.ngrok-free.dev -> https://localhost:7249
```

**Copy URL:** `https://abc123.ngrok-free.dev`

### Bước 4: Cấu hình Ngrok (Quan trọng!)

**Vấn đề:** Ngrok free có warning page, cần bypass

**Giải pháp 1: Thêm header trong request (Tự động)**
- Code đã được cấu hình để tự động xử lý

**Giải pháp 2: Dùng ngrok config (Khuyến nghị)**

Tạo file `ngrok.yml` trong thư mục home:
```yaml
version: "2"
authtoken: YOUR_AUTHTOKEN
tunnels:
  chatapp:
    addr: 7249
    proto: http
    inspect: false
    request_header:
      add:
        - "ngrok-skip-browser-warning: true"
```

**Chạy với config:**
```bash
ngrok start chatapp
```

### Bước 5: Test từ máy khác (Bất kỳ đâu)

1. **Mở browser trên máy Client**
2. **Truy cập:** `https://abc123.ngrok-free.dev` (URL từ Ngrok)
3. **Nếu có warning page:**
   - Click "Visit Site"
   - Hoặc thêm header: `ngrok-skip-browser-warning: true` (đã tự động)
4. **Đăng ký/Đăng nhập và test chat!**

---

## 3. Troubleshooting

### ❌ Lỗi 404 khi dùng Ngrok

**Nguyên nhân:**
- Ngrok forward sai port
- Server chưa chạy
- URL không đúng

**Giải pháp:**

1. **Kiểm tra server đang chạy:**
   ```bash
   # Terminal Server
   # Phải thấy: "Now listening on: https://localhost:7249"
   ```

2. **Kiểm tra Ngrok đang forward đúng:**
   ```bash
   # Terminal Ngrok
   # Phải thấy: "Forwarding https://xxx.ngrok-free.dev -> https://localhost:7249"
   ```

3. **Test trực tiếp:**
   - Mở: `https://localhost:7249` trên máy Server → Phải OK
   - Mở: `https://xxx.ngrok-free.dev` trên máy Client → Phải OK

4. **Kiểm tra Ngrok dashboard:**
   - Vào: https://dashboard.ngrok.com/status/tunnels
   - Xem tunnel có đang active không

### ❌ Lỗi CORS

**Triệu chứng:**
- Browser console: "CORS policy blocked"
- API requests fail

**Giải pháp:**
- Code đã được cấu hình CORS `AllowAll` cho development
- Nếu vẫn lỗi, kiểm tra `Program.cs` có `app.UseCors("AllowAll")`

### ❌ Lỗi Certificate (HTTPS)

**Triệu chứng:**
- Browser cảnh báo "Not secure"
- Không thể kết nối

**Giải pháp:**

**Với LAN:**
- Click "Advanced" → "Proceed" (development certificate)

**Với Ngrok:**
- Ngrok tự động cung cấp HTTPS certificate
- Nếu vẫn lỗi, thử dùng HTTP:
  ```bash
  ngrok http 5187  # Forward HTTP port thay vì HTTPS
  ```

### ❌ SignalR không kết nối

**Triệu chứng:**
- Chat không real-time
- Console: "WebSocket connection failed"

**Giải pháp:**

1. **Kiểm tra SignalR endpoint:**
   - URL phải là: `https://xxx.ngrok-free.dev/chatHub`
   - Không phải: `https://xxx.ngrok-free.dev/chatHub/`

2. **Kiểm tra CORS cho SignalR:**
   - `Program.cs` phải có: `.RequireCors("SignalRCors")`

3. **Test WebSocket:**
   - Mở browser console (F12)
   - Xem có lỗi WebSocket không

### ❌ Ngrok warning page

**Vấn đề:** Ngrok free hiển thị warning page trước khi vào site

**Giải pháp:**

**Cách 1: Dùng ngrok config (Khuyến nghị)**
```yaml
# ngrok.yml
tunnels:
  chatapp:
    addr: 7249
    proto: http
    request_header:
      add:
        - "ngrok-skip-browser-warning: true"
```

**Cách 2: Thêm header trong code (Đã tự động)**
- Code đã tự động thêm header khi detect Ngrok domain

**Cách 3: Upgrade Ngrok (Paid)**
- Ngrok paid không có warning page

---

## 🎯 Quick Start - Test với Ngrok

### Máy Server:

```bash
# Terminal 1
cd ChatApp/ChatApp
dotnet run

# Terminal 2
ngrok http 7249
# Copy URL: https://abc123.ngrok-free.dev
```

### Máy Client:

1. Mở browser
2. Truy cập: `https://abc123.ngrok-free.dev`
3. Click "Visit Site" nếu có warning
4. Test đăng ký/đăng nhập/chat!

---

## 📝 Checklist

### Trước khi test:

- [ ] Server đang chạy (`dotnet run`)
- [ ] Ngrok đang chạy (`ngrok http 7249`)
- [ ] Firewall đã mở port (nếu test LAN)
- [ ] CORS đã được cấu hình
- [ ] Database đã setup

### Khi test:

- [ ] Server console không có lỗi
- [ ] Ngrok dashboard shows active tunnel
- [ ] Browser console không có CORS error
- [ ] SignalR connection established
- [ ] API requests thành công

---

## 💡 Tips

1. **Dùng Ngrok cho test nhanh:** Dễ setup, không cần cấu hình router
2. **Dùng LAN cho test local:** Nhanh hơn, không phụ thuộc internet
3. **Dùng Port Forwarding cho production:** Ổn định, không giới hạn
4. **Monitor Ngrok dashboard:** Xem requests và errors real-time

---

## 🔗 Links hữu ích

- Ngrok Dashboard: https://dashboard.ngrok.com
- Ngrok Docs: https://ngrok.com/docs
- Test WebSocket: https://www.websocket.org/echo.html

---

**Chúc bạn test thành công! 🚀**

