using FeedbackAnalysis.FakeFeedbacksService.BackgroundWork;
using FeedbackAnalysis.FakeFeedbacksService.Services;

var builder = WebApplication.CreateBuilder(args);

// Общий потокобезопасный Random на весь сервис — генератор живёт как singleton
builder.Services.AddSingleton(Random.Shared);

builder.Services.AddSingleton<IFakeFeedbacksGenerator, FakeFeedbacksGenerator>();

builder.Services.AddHttpClient<IFeedbacksPublisher, FeedbacksPublisher>();

builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Таймер генерации включается ключом конфига/env-переменной Generator__TimerEnabled=true
if (builder.Configuration.GetSection("Generator").GetValue<bool>("TimerEnabled"))
{
    builder.Services.AddHostedService<GenerationTimerService>();
}

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));

app.Run();

namespace FeedbackAnalysis.FakeFeedbacksService
{
    public partial class Program { }
}
