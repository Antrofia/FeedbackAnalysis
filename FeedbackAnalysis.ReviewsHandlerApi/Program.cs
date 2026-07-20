using FeedbackAnalysis.ReviewsHandlerApi.Services;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<ISentimentService, SentimentService>();

builder.Services.AddControllers();

var app = builder.Build();

var process = new Process()
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "/bin/bash",
        Arguments = "python3 /app/reviews_predicate.py",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    }
};

process.Start();



// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
