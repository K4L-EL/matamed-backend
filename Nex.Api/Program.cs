using Nex.Api.Services;
using Nex.Api.Services.Stubs;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://+:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "NEX Health Intelligence API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("PostgreSQL connection configured.");
}
else
{
    Console.WriteLine("No database connection string found — running with in-memory stub data.");
}

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

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
