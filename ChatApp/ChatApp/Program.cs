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

// Log connection string (ẩn password)
var safeConnectionString = connectionString.Contains("Password=") 
    ? connectionString.Substring(0, connectionString.IndexOf("Password=")) + "Password=***"
    : connectionString;
Console.WriteLine($"[Database] Using connection: {safeConnectionString}");

// Phát hiện loại database dựa trên connection string
if (connectionString.Contains("Server=") || connectionString.Contains("(localdb)"))
{
    // SQL Server
    try
    {
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        Console.WriteLine("[Database] Configured for SQL Server");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database] Error configuring SQL Server: {ex.Message}");
        Console.WriteLine("[Database] Falling back to SQLite...");
        connectionString = "Data Source=chat.db";
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));
    }
}
else
{
    // SQLite (default)
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
    Console.WriteLine("[Database] Configured for SQLite");
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
            Console.WriteLine("[Database] SQLite database created/verified successfully");
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
                Console.WriteLine("[Database] SQL Server connection successful");
            }
            else
            {
                Console.WriteLine("[Database] Warning: Cannot connect to SQL Server. Please check:");
                Console.WriteLine("  1. SQL Server service is running");
                Console.WriteLine("  2. Connection string is correct");
                Console.WriteLine("  3. Database exists (run SetupDatabase.sql)");
                Console.WriteLine("  4. Firewall allows port 1433");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Database] Error connecting to SQL Server: {ex.Message}");
            Console.WriteLine("[Database] Please check connection string and SQL Server configuration");
        }
    }
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
