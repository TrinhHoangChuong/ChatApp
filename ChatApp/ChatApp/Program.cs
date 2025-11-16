using System;
using ChatApp.Data;
using ChatApp.Services;
using Microsoft.EntityFrameworkCore;
using ChatApp.Hubs;

// Database schema được tạo bằng:
// - SQL Server: Chạy script SetupDatabase.sql trong SSMS
// - SQLite: EF Core migrations sẽ tự động tạo

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddSignalR();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<MessageRepository>();
builder.Services.AddScoped<MessageService>();

// Cấu hình database: SQL Server hoặc SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=chat.db"; // Fallback về SQLite nếu không có connection string

// Phát hiện loại database dựa trên connection string
if (connectionString.Contains("Server=") || connectionString.Contains("(localdb)"))
{
    // SQL Server
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // SQLite (default)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Database setup
// Với SQL Server: Chạy script Database/SetupDatabase.sql trong SSMS trước khi chạy app
// Với SQLite: EF Core migrations sẽ tự động tạo schema
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Chỉ tự động tạo schema cho SQLite
    // SQL Server: KHÔNG dùng EnsureCreated(), phải chạy script SetupDatabase.sql trước
    var isSqlite = connectionString.Contains("Data Source=") && !connectionString.Contains("Server=");
    
    if (isSqlite)
    {
        // Chỉ chạy với SQLite
        try
        {
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            // Log error nhưng không crash app
            Console.WriteLine($"Warning: Could not ensure database created: {ex.Message}");
        }
    }
    // Với SQL Server, schema đã được tạo bằng script SetupDatabase.sql
    // Không cần làm gì ở đây
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.MapFallbackToFile("index.html");

app.Run();
