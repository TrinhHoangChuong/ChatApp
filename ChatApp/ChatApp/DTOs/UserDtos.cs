namespace ChatApp.DTOs
{
    // Dữ liệu cơ bản của người dùng
    public class UserBaseDto
    {
        public string Username { get; set; } = string.Empty;
    }

    // Khi client đăng ký (có thêm mật khẩu)
    public class UserCreateDto : UserBaseDto
    {
        public string Password { get; set; } = string.Empty;
    }

    // Khi server trả thông tin người dùng
    public class UserDto : UserBaseDto
    {
        public int Id { get; set; }
    }
}