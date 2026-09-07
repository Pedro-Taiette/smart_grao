using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using SmartGrao.Application;
using SmartGrao.Infrastructure;
using SmartGrao.Infrastructure.Persistence;
using SmartGrao.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration).WriteTo.Console());

// ── Camadas ──────────────────────────────────────────────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// ── API ──────────────────────────────────────────────────────────────────────
// Enums como texto ("Soja", nao 1): legivel no contrato e estavel quando alguem reordenar o enum.
// Configurado nas duas pontas — no MVC (execucao) e no HTTP JSON (que e o que o gerador de esquema
// OpenAPI le) — porque ajustar so uma produz um documento que mente sobre o que trafega.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

// CORS para o SPA. O navegador bloqueia a chamada cross-origin sem isso; quem manda continua sendo
// a API, entao aqui so se decide de quais origens o navegador tem permissao de partir.
const string PoliticaSpa = "spa";
var origensPermitidas = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options => options.AddPolicy(PoliticaSpa, policy => policy
    .WithOrigins(origensPermitidas)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

var app = builder.Build();

// Migrations sao um passo explicito (`dotnet run -- migrate`), nunca automaticas na subida: uma
// mudanca de esquema continua sendo algo que alguem executa e observa, em vez de acontecer no meio
// de um deploy que ninguem estava acompanhando.
if (args.Contains("migrate"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<SmartGraoDbContext>().Database.MigrateAsync();
    return;
}

app.UseExceptionHandler();

// CORS antes do roteamento, para que o preflight OPTIONS seja respondido pelo proprio middleware.
app.UseCors(PoliticaSpa);
app.UseRouting();

app.MapOpenApi();
if (!app.Environment.IsProduction())
    app.MapScalarApiReference();

app.MapControllers();

await app.RunAsync();
