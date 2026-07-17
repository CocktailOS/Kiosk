using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Endpoints;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDirectory);
var connectionString = builder.Configuration.GetConnectionString("CocktailOs")
    ?? $"Data Source={Path.Combine(dataDirectory, "cocktailos.db")}";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient<ApplicationUpdateService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CocktailOS-Kiosk-Updater");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddSingleton<DummyPumpOutput>();
builder.Services.AddSingleton<GpioPumpOutput>();
builder.Services.AddSingleton<IPumpOutput, PumpOutputRouter>();
builder.Services.AddSingleton<DispenseService>();
builder.Services.AddSingleton<NetworkAccessPolicy>();
builder.Services.AddSingleton<NetworkAccessPinService>();
builder.Services.AddSingleton<NetworkAccessSessionService>();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");
    logger.LogError(exception, "Unbehandelter Fehler bei {Method} {Path}", context.Request.Method, context.Request.Path);

    var status = exception switch
    {
        BadHttpRequestException => StatusCodes.Status400BadRequest,
        DbUpdateException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = status,
        Title = status switch
        {
            StatusCodes.Status400BadRequest => "Ungültige Anfrage",
            StatusCodes.Status409Conflict => "Datensatz kann nicht gespeichert werden",
            _ => "Interner Serverfehler"
        },
        Detail = status switch
        {
            StatusCodes.Status400BadRequest => "Der Anfrageinhalt ist kein gültiges UTF-8-JSON.",
            StatusCodes.Status409Conflict => "Name, gpio-Pin, Volumen oder Zuordnung ist bereits vergeben oder wird noch verwendet.",
            _ => "Die Anfrage konnte nicht verarbeitet werden. Details wurden protokolliert."
        }
    });
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var remoteAddress = context.Connection.RemoteIpAddress;
    var isRemoteRequest = remoteAddress is not null && !IPAddress.IsLoopback(remoteAddress);
    if (isRemoteRequest && !app.Services.GetRequiredService<NetworkAccessPolicy>().IsEnabled)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Netzwerkzugriff deaktiviert",
            Detail = "Diese CocktailOS-Instanz ist nur lokal erreichbar. Netzwerkzugriff kann direkt am Gerät in den Systemeinstellungen aktiviert werden."
        });
        return;
    }

    if (isRemoteRequest && !IsPublicNetworkAccessPath(context.Request.Path) && !app.Services.GetRequiredService<NetworkAccessSessionService>().IsValid(context.Request.Cookies["CocktailOS.NetworkAccess"]))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "PIN erforderlich",
            Detail = "Bitte gib den Netzwerk-PIN ein, um CocktailOS von diesem Gerät zu verwenden."
        });
        return;
    }

    var path = context.Request.Path;
    if (path == "/" || path == "/index.html" || path == "/app.css" || path == "/app.js")
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-cache, no-store";
            return Task.CompletedTask;
        });
    }

    await next();
});
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapCocktailOsApi();
app.MapFallbackToFile("index.html");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(db);
    var configuration = await db.MachineConfigurations.AsNoTracking().SingleAsync(x => x.Id == CocktailOS.Kiosk.Models.MachineConfiguration.SingletonId);
    scope.ServiceProvider.GetRequiredService<NetworkAccessPolicy>().SetEnabled(configuration.NetworkAccessEnabled);
}

await app.RunAsync();

static bool IsPublicNetworkAccessPath(PathString path) =>
    path == "/" || path == "/index.html" || path == "/app.css" || path == "/tokens.css" || path == "/app.js"
    || path.StartsWithSegments("/assets") || path == "/api/network-access/status" || path == "/api/network-access/authenticate";

public partial class Program;
