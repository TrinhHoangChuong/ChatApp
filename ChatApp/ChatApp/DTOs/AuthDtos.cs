namespace ChatApp.DTOs
{
    // Khi đăng nhập hoặc đăng ký thành công → server trả token
    public class TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "bearer";
    }

    // Khi xác thực token → dùng để lưu thông tin người dùng trong token
    public class TokenDataDto
    {
        public string? Username { get; set; }
    }
}
    