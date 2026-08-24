using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Repositories.EF;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;
using FeedbackAnalysis.DataApi.Services;
using FeedbackAnalysis.DataApi.UnitOfWork;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<EFContext>(ops => ops.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteTest")));

builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
builder.Services.AddScoped<IFeedbackAnswerStatusRepository, FeedbackAnswerStatusRepository>();
builder.Services.AddScoped<IFeedbackTonalityRepository, FeedbackTonalityRepository>();
builder.Services.AddScoped<IFeedbackAnswerRepository, FeedbackAnswerRepository>();
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();

builder.Services.AddScoped<IFeedbacksService, FeedbacksService>();

// Typed-клиент ML-сервиса тональности.
builder.Services.AddHttpClient<ITonalityService, TonalityService>(client =>
{
    var baseUrl = builder.Configuration.GetSection("Services")["FeedbacksHandler"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/");
    }
});

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (string.IsNullOrWhiteSpace(builder.Configuration.GetSection("Services")["FeedbacksHandler"]))
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(
        "Не задан адрес ML-сервиса (Services:FeedbacksHandler) — классификация тональности будет падать. " +
        "Укажите его в appsettings.json или через переменную окружения Services__FeedbacksHandler.");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EFContext>();
    db.Database.EnsureCreated();
}

app.Run();

namespace FeedbackAnalysis.DataApi
{
    public partial class Program { }
}
