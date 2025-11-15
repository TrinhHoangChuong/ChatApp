namespace ChatApp.DTOs
{
    // Khi upload file thành công → trả về thông tin file
    public class FileResponseDto
    {
        public string Filename { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Uploader { get; set; } = string.Empty;
    }
}
