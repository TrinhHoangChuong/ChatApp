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

// Log connection string (ẩn password để debug)
var safeLogString = connectionString;
if (connectionString.Contains("Password="))
{
    var pwdIndex = connectionString.IndexOf("Password=");
    var endIndex = connectionString.IndexOf(";", pwdIndex);
    if (endIndex == -1) endIndex = connectionString.Length;
    safeLogString = connectionString.Substring(0, pwdIndex) + "Password=***" + connectionString.Substring(endIndex);
}
Console.WriteLine($"[Config] Database connection: {safeLogString}");

// Phát hiện loại database dựa trên connection string
if (connectionString.Contains("Server=") || connectionString.Contains("(localdb)"))
{
    // SQL Server
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
    Console.WriteLine("[Config] Using SQL Server");
}
else
{
    // SQLite (default)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
    Console.WriteLine("[Config] Using SQLite");
}

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cấu hình CORS để cho phép requests từ bất kỳ origin nào (cho development và Ngrok)
builder.Services.AddCors(options =>
{
    // CORS cho API và static files
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    // CORS cho SignalR - không dùng AllowCredentials với AllowAnyOrigin
    // SignalR sẽ hoạt động với AllowAnyOrigin (không cần credentials cho WebSocket)
    options.AddPolicy("SignalRCors", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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
            Console.WriteLine("[Database] SQLite database ready");
        }
        catch (Exception ex)
        {
            // Log error nhưng không crash app
            Console.WriteLine($"[Database] Warning: Could not ensure database created: {ex.Message}");
        }
    }
    else
    {
        // Với SQL Server, kiểm tra kết nối
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            if (canConnect)
            {
                Console.WriteLine("[Database] SQL Server connection OK");
            }
            else
            {
                Console.WriteLine("[Database] ⚠️  Cannot connect to SQL Server!");
                Console.WriteLine("[Database] Please check:");
                Console.WriteLine("[Database]   1. SQL Server service is running");
                Console.WriteLine("[Database]   2. Connection string in appsettings.json");
                Console.WriteLine("[Database]   3. Database exists (run SetupDatabase.sql)");
                Console.WriteLine("[Database]   4. Run DetectSQLServer.ps1 to find correct connection string");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] ❌ SQL Server connection error: {ex.Message}");
            Console.WriteLine("[Database] Run 'DetectSQLServer.ps1' to find correct connection string");
            Console.WriteLine("[Database] Or use SQLite: Change connection string to 'Data Source=chat.db'");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS phải được gọi trước UseHttpsRedirection
app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub").RequireCors("SignalRCors");
app.MapFallbackToFile("index.html");

app.Run();
