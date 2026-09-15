using System.Text.Json.Serialization;
using MealPenalty.Application;
using MealPenalty.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "WebClient";

// Render (and most free PaaS hosts) tell the app which port to bind via $PORT rather than a
// fixed one - Kestrel needs to listen there explicitly, not just on the dev-time default.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allowed origins come from config (env var CORS__ALLOWEDORIGINS or CORS:AllowedOrigins in
// appsettings), comma-separated, so the deployed frontend's URL never has to be hardcoded here.
// Falls back to the local Vite dev origins when unset.
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDirectory);
var dbPath = Path.Combine(dataDirectory, "mealpenalty.db");
var connectionString = builder.Configuration.GetConnectionString("MealPenaltyDb") ?? $"Data Source={dbPath}";

builder.Services.AddMealPenaltyApplication();
builder.Services.AddMealPenaltyInfrastructure(connectionString);

var app = builder.Build();

app.Services.InitializeMealPenaltyDatabase();
app.Logger.LogInformation("MealPenalty database: {ConnectionString}", connectionString);

// Swagger stays on in production too - this is a public demo, not a system with anything secret
// to hide behind an environment check.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(CorsPolicy);
app.UseAuthorization();
app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
// Render's health check defaults to "/" unless a custom path is configured on the service - keep
// this responding 200 so a deploy never hangs waiting on a path we never intended to serve.
app.MapGet("/", () => Results.Ok(new { service = "MealPenalty.Api", status = "ok" }));

app.Run();

public partial class Program;
