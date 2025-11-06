using ChatApp.Data;
using ChatApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ✅ Thêm AuthService và UserRepository
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserRepository>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=chat.db"));

// ✅ Cấu hình JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YOUR_SUPER_SECRET_KEY_CHANGE_THIS";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ChatApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ChatAppUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero // Remove clock skew for token expiration
        };
        
        // Add event handler to log authentication failures
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT Authentication Failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"JWT Token Validated for user: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"JWT Challenge: {context.Error} - {context.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ✅ CORS để frontend có thể gọi API
builder.Services.AddCors(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Trong Development: cho phép localhost
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:5187", "https://localhost:7249")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    }
    else
    {
        // Trong Production: chỉ cho phép domain cụ thể
        options.AddDefaultPolicy(policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { "https://yourdomain.com" };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    }
});

// Swagger (API test UI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ✅ Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        Console.WriteLine("🔄 Checking for pending migrations...");
        
        // Check if database exists
        var canConnect = db.Database.CanConnect();
        Console.WriteLine($"📊 Database connection: {(canConnect ? "✅ Connected" : "⚠️  Database does not exist, will be created")}");
        
        // Get pending migrations
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            Console.WriteLine($"📋 Found {pendingMigrations.Count} pending migration(s):");
            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($"   - {migration}");
            }
        }
        
        // Get applied migrations
        var appliedMigrations = db.Database.GetAppliedMigrations().ToList();
        if (appliedMigrations.Any())
        {
            Console.WriteLine($"✅ Applied migrations ({appliedMigrations.Count}):");
            foreach (var migration in appliedMigrations)
            {
                Console.WriteLine($"   ✓ {migration}");
            }
        }
        
        // Apply migrations
        if (pendingMigrations.Any())
        {
            Console.WriteLine("🔄 Applying migrations...");
            db.Database.Migrate();
            Console.WriteLine("✅ All migrations applied successfully!");
        }
        else
        {
            Console.WriteLine("✅ Database is up to date. No pending migrations.");
        }
        
        // Verify tables exist by trying to query them
        var tables = new[] { "Users", "Messages", "Rooms", "RoomMembers" };
        Console.WriteLine("\n📊 Checking database tables:");
        foreach (var tableName in tables)
        {
            try
            {
                // Try to get row count - if table exists, this will work
                db.Database.ExecuteSqlRaw($"SELECT COUNT(*) FROM {tableName}");
                Console.WriteLine($"   ✅ Table '{tableName}': exists");
            }
            catch
            {
                Console.WriteLine($"   ❌ Table '{tableName}': MISSING");
            }
        }
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error applying migrations: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        // Don't exit - let the app start anyway so user can see the error
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // ✅ Chỉ redirect HTTPS trong Production
    app.UseHttpsRedirection();
    
    // ✅ Thêm HSTS (HTTP Strict Transport Security) trong Production
    app.UseHsts();
}

// ✅ Thêm Security Headers
app.Use(async (context, next) =>
{
    // Không thêm security headers cho Swagger trong development
    if (context.Request.Path.StartsWithSegments("/swagger"))
    {
        await next();
        return;
    }

    // Security Headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Chỉ thêm CSP và HSTS trong Production
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }

    await next();
});

// ✅ CORS - Phải đặt trước Authentication
app.UseCors();

// ✅ Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Serve static files (HTML, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Default route to index.html
app.MapFallbackToFile("index.html");

app.Run();
