using FeedbackAnalysis.WBService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IFeedbackParser, FeedbackParser>();
builder.Services.AddSingleton<IFeedbacksDataService, FeedbacksDataService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
