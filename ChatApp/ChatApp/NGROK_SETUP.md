# 🚀 Hướng dẫn Setup Ngrok cho ChatApp

## ⚠️ Vấn đề: Lỗi 404 khi dùng Ngrok

Nếu bạn gặp lỗi 404 khi truy cập qua Ngrok, đây là cách fix:

---

## 📋 Bước 1: Kiểm tra Server đang chạy

**Terminal 1 - Chạy ChatApp:**
```bash
cd ChatApp/ChatApp
dotnet run
```

**Kết quả phải thấy:**
```
Now listening on: https://localhost:7249
Now listening on: http://localhost:5187
```

**Test local trước:**
- Mở: `https://localhost:7249` → Phải OK
- Nếu không OK, fix lỗi trước khi dùng Ngrok

---

## 📋 Bước 2: Cài đặt và Cấu hình Ngrok

### 2.1. Download Ngrok

**Windows:**
- Download: https://ngrok.com/download
- Hoặc: `choco install ngrok`

**Mac:**
```bash
brew install ngrok
```

**Linux:**
```bash
# Download và extract
wget https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-linux-amd64.tgz
tar -xzf ngrok-v3-stable-linux-amd64.tgz
sudo mv ngrok /usr/local/bin/
```

### 2.2. Đăng ký và lấy Authtoken

1. **Đăng ký:** https://dashboard.ngrok.com/signup
2. **Lấy authtoken:** https://dashboard.ngrok.com/get-started/your-authtoken
3. **Cấu hình:**
   ```bash
   ngrok config add-authtoken YOUR_AUTHTOKEN
   ```

---

## 📋 Bước 3: Chạy Ngrok

### 3.1. Chạy Ngrok cơ bản

**Terminal 2 - Chạy Ngrok:**
```bash
ngrok http 7249
```

**Kết quả:**
```
Forwarding   https://abc123.ngrok-free.dev -> https://localhost:7249
```

**Copy URL:** `https://abc123.ngrok-free.dev`

### 3.2. Chạy Ngrok với config (Khuyến nghị - Bypass warning)

**Tạo file `ngrok.yml`** (trong thư mục home hoặc project):

**Windows:** `C:\Users\YourName\ngrok.yml`
**Mac/Linux:** `~/.ngrok2/ngrok.yml` hoặc `~/ngrok.yml`

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

---

## 📋 Bước 4: Test từ máy khác

1. **Mở browser trên máy Client**
2. **Truy cập:** `https://abc123.ngrok-free.dev`
3. **Nếu có warning page:**
   - Click "Visit Site"
   - Hoặc dùng config ở trên để bypass
4. **Test đăng ký/đăng nhập/chat!**

---

## 🔧 Troubleshooting

### ❌ Lỗi 404

**Nguyên nhân:**
- Ngrok forward sai port
- Server chưa chạy
- URL không đúng

**Giải pháp:**

1. **Kiểm tra server:**
   ```bash
   # Terminal Server
   # Phải thấy: "Now listening on: https://localhost:7249"
   ```

2. **Kiểm tra Ngrok:**
   ```bash
   # Terminal Ngrok
   # Phải thấy: "Forwarding https://xxx.ngrok-free.dev -> https://localhost:7249"
   ```

3. **Test trực tiếp:**
   - Server: `https://localhost:7249` → OK
   - Client: `https://xxx.ngrok-free.dev` → Phải OK

4. **Kiểm tra Ngrok dashboard:**
   - Vào: https://dashboard.ngrok.com/status/tunnels
   - Tunnel phải "Active"

### ❌ Ngrok warning page

**Vấn đề:** Ngrok free hiển thị warning page

**Giải pháp:**

**Cách 1: Dùng config (Khuyến nghị)**
```yaml
# ngrok.yml
request_header:
  add:
    - "ngrok-skip-browser-warning: true"
```

**Cách 2: Thêm header trong browser (Manual)**
- Mở DevTools (F12)
- Network tab → Add custom header
- Header: `ngrok-skip-browser-warning: true`

**Cách 3: Upgrade Ngrok (Paid)**
- Không có warning page

### ❌ CORS Error

**Triệu chứng:**
- Browser console: "CORS policy blocked"
- API requests fail

**Giải pháp:**
- Code đã được cấu hình CORS `AllowAll`
- Nếu vẫn lỗi, restart server sau khi thêm CORS

### ❌ SignalR không kết nối

**Triệu chứng:**
- Chat không real-time
- Console: "WebSocket connection failed"

**Giải pháp:**

1. **Kiểm tra SignalR endpoint:**
   - URL: `https://xxx.ngrok-free.dev/chatHub`
   - Không phải: `https://xxx.ngrok-free.dev/chatHub/`

2. **Kiểm tra CORS:**
   - `Program.cs` phải có: `.RequireCors("SignalRCors")`

3. **Test WebSocket:**
   - Mở browser console (F12)
   - Xem có lỗi WebSocket không

---

## 🎯 Quick Start

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

1. Mở: `https://abc123.ngrok-free.dev`
2. Click "Visit Site" nếu có warning
3. Test!

---

## 📝 Checklist

- [ ] Server đang chạy (`dotnet run`)
- [ ] Ngrok đang chạy (`ngrok http 7249`)
- [ ] Ngrok URL hoạt động (test trên máy Server trước)
- [ ] CORS đã được cấu hình
- [ ] SignalR endpoint đúng (`/chatHub`)

---

## 💡 Tips

1. **Dùng Ngrok config:** Bypass warning page tự động
2. **Monitor dashboard:** Xem requests và errors
3. **Test local trước:** Đảm bảo server OK trước khi dùng Ngrok
4. **Dùng paid Ngrok:** Không có warning, ổn định hơn

---

**Chúc bạn test thành công! 🚀**

