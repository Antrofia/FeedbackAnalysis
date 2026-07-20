using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Services;
using FeedbackAnalysis.DataApi.UnitOfWork;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddDbContext<EFContext>(ops => ops.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Transient);
builder.Services.AddDbContext<EFContext>(ops => ops.UseSqlite(builder.Configuration.GetConnectionString("SqlLiteTest")), ServiceLifetime.Transient);

builder.Services.AddTransient<IUnitOfWork, EFUnitOfWork>();
builder.Services.AddTransient<IFeedbacksService, FeedbacksService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
