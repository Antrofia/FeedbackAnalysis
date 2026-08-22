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
builder.Services.AddScoped<IUnitOfWork, EFUnitOfWork>();

builder.Services.AddScoped<IFeedbacksService, FeedbacksService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

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
