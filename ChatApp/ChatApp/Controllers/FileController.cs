using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FileController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không có file nào được tải lên.");

            // Tạo thư mục lưu file: wwwroot/uploads/
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Đường dẫn lưu file
            var filePath = Path.Combine(uploadsPath, file.FileName);

            // Ghi file xuống ổ đĩa
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về URL cho client
            var fileUrl = $"{Request.Scheme}://{Request.Host}/uploads/{file.FileName}";
            return Ok(new { file_url = fileUrl });
        }
    }
}
