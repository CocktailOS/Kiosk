using CocktailOS.Kiosk.Data;
using CocktailOS.Kiosk.Endpoints;
using CocktailOS.Kiosk.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            StatusCodes.Status409Conflict => "Name, GPIO-Pin, Volumen oder Zuordnung ist bereits vergeben oder wird noch verwendet.",
            _ => "Die Anfrage konnte nicht verarbeitet werden. Details wurden protokolliert."
        }
    });
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "no-cache, no-store";
        }
    }
});

app.MapCocktailOsApi();
app.MapFallbackToFile("index.html");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseInitializer.InitializeAsync(db);
}

await app.RunAsync();

public partial class Program;
