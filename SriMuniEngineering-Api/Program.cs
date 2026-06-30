using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using SriMuniEngineering_Api.Features.Auth;
using SriMuniEngineering_Api.Features.Customers;
using SriMuniEngineering_Api.Features.Dashboard;
using SriMuniEngineering_Api.Features.EWayBill;
using SriMuniEngineering_Api.Features.Products;
using SriMuniEngineering_Api.Features.InspectionReports;
using SriMuniEngineering_Api.Features.Invoices;
using SriMuniEngineering_Api.Features.Quotations;
using SriMuniEngineering_Api.Features.StockLedger;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Security;
using SriMuniEngineering_Api.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// ─── QuestPDF License ─────────────────────────────────────────────
QuestPDF.Settings.License = LicenseType.Community;

// ─── Database ─────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── JWT Authentication ───────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ─── Controllers with Global Authorize Filter ─────────────────────
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// ─── OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ─── Security Services ────────────────────────────────────────────
builder.Services.AddSingleton<JwtTokenGenerator>();
builder.Services.AddSingleton<TokenBlacklistService>();

// ─── Feature Services ─────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<QuotationService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<InspectionReportService>();
builder.Services.AddScoped<EWayBillService>();

// ─── Supabase Storage HttpClient ──────────────────────────────────
builder.Services.AddHttpClient<SupabaseStorageService>();

// ─── CORS ─────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:4200", "https://your-published-url-placeholder.com" };
            
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ─── Middleware Pipeline ──────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
