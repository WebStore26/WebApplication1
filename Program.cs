using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using Npgsql.EntityFrameworkCore.PostgreSQL; // Add this using directive

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var logger = LoggerFactory
    .Create(logging => logging.AddConsole())
    .CreateLogger("Startup");

logger.LogInformation("Starting");

builder.Services.AddControllers();

string connectionString = "";
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(dbUrl))
{
    connectionString = ConvertDatabaseUrl(dbUrl);
}
else
{
    connectionString = builder.Configuration["App:ConnectionString"];
    //connectionString = @"Host=nozomi.proxy.rlwy.net;Port=42425;Database=railway;Username=postgres;Password=AoIBESWkuevWLENtMiZxQsMNMgknsIYq;";
}

builder.Services.AddDbContext<AppDb>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.UseStaticFiles(); 

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();

static string ConvertDatabaseUrl(string rawUrl)
{
    // railway example:
    // postgres://user:pass@host:port/dbname

    if (!rawUrl.StartsWith("postgres://") && !rawUrl.StartsWith("postgresql://"))
        throw new Exception("Invalid DATABASE_URL format");

    // Remove protocol
    string url = rawUrl.Replace("postgres://", "").Replace("postgresql://", "");

    // Split user:pass@host:port/db
    var atIndex = url.IndexOf("@");
    var userPass = url.Substring(0, atIndex);
    var hostPortDb = url.Substring(atIndex + 1);

    // user + pass
    var user = userPass.Split(':')[0];
    var pass = userPass.Split(':')[1];

    // host + port + db
    var parts = hostPortDb.Split('/');
    var hostPort = parts[0];
    var db = parts[1];

    string host;
    string port;

    if (hostPort.Contains(":"))
    {
        host = hostPort.Split(':')[0];
        port = hostPort.Split(':')[1];
    }
    else
    {
        host = hostPort;
        port = "5432"; // default fallback
    }

    return $"Host={host};Port={port};Username={user};Password={pass};Database={db};SSL Mode=Require;Trust Server Certificate=true;";
}
