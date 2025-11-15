# 🐛 HƯỚNG DẪN DEBUG LỖI

## ✅ ĐÃ SỬA CÁC LỖI

### 1. Error Handling
- ✅ Thêm try-catch vào tất cả endpoints trong `RoomController`
- ✅ Thêm try-catch vào `MessageController.Create`
- ✅ Validation userId từ JWT token (sử dụng TryParse thay vì Parse)
- ✅ Validation null checks cho các objects

### 2. Cải thiện Error Messages
- ✅ Trả về error message chi tiết trong response
- ✅ Kiểm tra userId hợp lệ trước khi sử dụng
- ✅ Xử lý null reference exceptions

---

## 🔍 CÁCH DEBUG

### Bước 1: Kiểm tra Console trong Browser (F12)

1. Mở DevTools: `F12` hoặc `Ctrl + Shift + I`
2. Chuyển sang tab **Console**
3. Xem các lỗi JavaScript:
   - `Failed to load rooms` → Lỗi khi load danh sách room
   - `Failed to create room` → Lỗi khi tạo room
   - `Failed to create/get DM` → Lỗi khi tạo chat riêng

### Bước 2: Kiểm tra Network Tab (F12)

1. Mở DevTools: `F12`
2. Chuyển sang tab **Network** (Mạng)
3. Refresh trang (F5)
4. Tìm các request đến `/api/room` hoặc `/api/message`
5. Click vào request bị lỗi (status code đỏ)
6. Xem tab **Response** để đọc error message từ server

**Ví dụ Response lỗi:**
```json
{
  "message": "Error loading rooms",
  "error": "Invalid user token"
}
```

### Bước 3: Kiểm tra Backend Logs

1. Mở terminal nơi chạy `dotnet run`
2. Xem các exception/error messages:
   ```
   fail: Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware
        An unhandled exception has occurred...
   ```

### Bước 4: Kiểm tra Token

Trong Browser Console (F12), chạy:
```javascript
// Kiểm tra token
localStorage.getItem('token')

// Nếu null hoặc undefined, cần đăng nhập lại
```

### Bước 5: Clear và Đăng nhập lại

Nếu token không hợp lệ:
```javascript
// Trong Console (F12)
localStorage.clear()
// Sau đó refresh trang và đăng nhập lại
```

---

## 🚨 CÁC LỖI THƯỜNG GẶP VÀ CÁCH SỬA

### Lỗi: "Invalid user token"

**Nguyên nhân:**
- Token không có claim `NameIdentifier` (userId)
- Token hết hạn
- Token bị lỗi format

**Giải pháp:**
1. Đăng nhập lại để lấy token mới
2. Kiểm tra `AuthService.CreateAccessToken` có đúng không

### Lỗi: "Error loading rooms"

**Nguyên nhân:**
- Database chưa có bảng Rooms
- UserId không hợp lệ
- Lỗi query database

**Giải pháp:**
```bash
# Chạy migration lại
cd ChatApp\ChatApp
dotnet ef database update
```

### Lỗi: "Error creating room"

**Nguyên nhân:**
- Room name trống
- Database constraint violation
- Lỗi save changes

**Giải pháp:**
- Kiểm tra tên room không được trống
- Kiểm tra database connection
- Xem backend logs để biết lỗi cụ thể

### Lỗi: "Error creating DM"

**Nguyên nhân:**
- Target user không tồn tại
- Lỗi khi tạo DM room
- Lỗi khi add members

**Giải pháp:**
- Kiểm tra targetUserId có đúng không
- Kiểm tra database có user đó không

---

## 📋 CHECKLIST DEBUG

Khi gặp lỗi, làm theo thứ tự:

- [ ] **Restart Backend**
  ```bash
  # Tắt backend (Ctrl+C)
  # Chạy lại
  dotnet run
  ```

- [ ] **Hard Refresh Browser**
  - `Ctrl + Shift + R` (Windows/Linux)
  - `Cmd + Shift + R` (Mac)

- [ ] **Clear localStorage**
  ```javascript
  localStorage.clear()
  ```

- [ ] **Đăng nhập lại**
  - Đăng xuất
  - Đăng nhập lại để lấy token mới

- [ ] **Kiểm tra Database**
  ```bash
  # Kiểm tra database có tồn tại không
  dir chat.db
  
  # Nếu không có, chạy migration
  dotnet ef database update
  ```

- [ ] **Kiểm tra Console (F12)**
  - Xem error messages trong Console tab
  - Xem Network tab để xem response từ server

- [ ] **Kiểm tra Backend Logs**
  - Xem terminal nơi chạy `dotnet run`
  - Tìm exception messages

---

## 🔧 TEST SAU KHI SỬA

Sau khi restart backend:

1. **Test Đăng nhập:**
   - Đăng nhập lại
   - Kiểm tra Console không có lỗi

2. **Test Load Rooms:**
   - Vào trang chat
   - Kiểm tra danh sách rooms hiển thị

3. **Test Tạo Room:**
   - Click "+ Tạo Room"
   - Nhập tên và tạo
   - Kiểm tra room xuất hiện

4. **Test Tạo DM:**
   - Click vào user trong danh sách
   - Kiểm tra DM được tạo

---

## 📝 GHI CHÚ

- Backend đã được cập nhật với error handling tốt hơn
- Error messages sẽ hiển thị chi tiết trong Network tab
- Tất cả lỗi sẽ được catch và trả về response có format

**Nếu vẫn gặp lỗi sau khi làm các bước trên, vui lòng:**
1. Copy error message từ Console (F12)
2. Copy response từ Network tab
3. Copy backend logs từ terminal

