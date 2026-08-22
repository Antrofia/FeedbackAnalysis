using FeedbackAnalysis.WBService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddSingleton<IFeedbackParser, FeedbackParser>();
builder.Services.AddHttpClient<IFeedbacksDataService, FeedbacksDataService>();
builder.Services.AddHttpClient<FeedbacksGeneratedHandler>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapControllers();

app.Run();
