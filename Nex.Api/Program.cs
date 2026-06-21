using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nex.Api.Data;
using Nex.Api.Services;
using Nex.Api.Services.Stubs;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MetaMed API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Paste your JWT as: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                if (origin.StartsWith("http://localhost:")) return true;
                return origin is "https://metamed.aymane.co.uk" or "https://app.metamed.aymane.co.uk";
            })
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (!string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("PostgreSQL connection configured.");
}
else
{
    Console.WriteLine("WARN: No database connection string found — falling back to local SQLite at /tmp/metamed.db");
    connectionString = null;
}

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (!string.IsNullOrEmpty(connectionString))
        opt.UseNpgsql(connectionString);
    else
        opt.UseNpgsql("Host=localhost;Port=5432;Database=metamed;Username=postgres;Password=postgres");
});

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("JWT signing key not configured. Set Jwt:Key or env JWT_KEY.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "metamed";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "metamed-client";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IJwtService, JwtService>();

builder.Services.AddSingleton<IDashboardService, StubDashboardService>();
builder.Services.AddSingleton<IInfectionService, StubInfectionService>();
builder.Services.AddSingleton<IPatientService, StubPatientService>();
builder.Services.AddSingleton<IOutbreakService, StubOutbreakService>();
builder.Services.AddSingleton<IForecastService, StubForecastService>();
builder.Services.AddSingleton<IAlertService, StubAlertService>();
builder.Services.AddSingleton<IScreeningService, StubScreeningService>();
builder.Services.AddSingleton<IResistanceService, StubResistanceService>();
builder.Services.AddSingleton<ITransmissionService, StubTransmissionService>();
builder.Services.AddSingleton<IDeviceService, StubDeviceService>();
builder.Services.AddSingleton<IPipelineService, StubPipelineService>();
builder.Services.AddSingleton<IAiChatService, AiChatService>();
builder.Services.AddSingleton<IReportService, ReportService>();

builder.Services.AddHttpClient<IOpenAIDirectClient, OpenAIDirectClient>();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations / ensure created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
        Console.WriteLine("Database schema ensured.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"WARN: database init failed: {ex.Message}");
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
