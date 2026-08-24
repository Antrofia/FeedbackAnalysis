using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Services;
using FeedbackAnalysis.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FeedbackAnalysis.Tests.DataApiTests;

/// <summary>
/// Фабрика DataApi с подменённой базой на общую SQLite in-memory
/// и заглушкой ML-сервиса тональности (primary handler у typed-клиента).
/// </summary>
public sealed class DataApiFactory : WebApplicationFactory<FeedbackAnalysis.DataApi.Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly StubHttpMessageHandler _mlStub = new(RespondLikeMlService);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EFContext>>();
            services.AddDbContext<EFContext>(options => options.UseSqlite(_connection));

            services.AddHttpClient<ITonalityService, TonalityService>(client =>
                {
                    client.BaseAddress = new Uri("http://ml-stub/");
                })
                .ConfigurePrimaryHttpMessageHandler(() => _mlStub);
        });
    }

    /// <summary>
    /// Имитация POST /predict и POST /predict/batch (модель blanchefort/rubert-base-cased-sentiment-rurewiews):
    /// класс определяется по ключевому слову в тексте.
    /// id2label модели: 0=NEUTRAL, 1=POSITIVE, 2=NEGATIVE.
    /// </summary>
    private static HttpResponseMessage RespondLikeMlService(HttpRequestMessage request)
    {
        var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "";

        using var doc = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);
        var root = doc.RootElement;

        var isBatch = request.RequestUri?.AbsolutePath.EndsWith("/predict/batch", StringComparison.OrdinalIgnoreCase) == true;

        if (isBatch)
        {
            var predictions = root.GetProperty("texts").EnumerateArray()
                .Select(item =>
                {
                    var text = item.GetString() ?? "";
                    var (label, confidence) = ClassifyByText(text);
                    return new { text, label, confidence };
                })
                .ToList();

            var batchJson = JsonSerializer.Serialize(new { results = predictions });

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(batchJson, Encoding.UTF8, "application/json")
            };
        }

        var singleText = root.TryGetProperty("text", out var element) ? element.GetString() ?? "" : "";
        var (singleLabel, singleConfidence) = ClassifyByText(singleText);

        var json = JsonSerializer.Serialize(new { text = singleText, label = singleLabel, confidence = singleConfidence });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static (int Label, double Confidence) ClassifyByText(string text)
    {
        var lowered = text.ToLowerInvariant();

        return lowered.Contains("ужасн") || lowered.Contains("плох")
            ? (2, 0.97)
            : lowered.Contains("отличн") || lowered.Contains("хорош")
                ? (1, 0.95)
                : (0, 0.9);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

public class DataApiIntegrationTests : IClassFixture<DataApiFactory>
{
    private readonly DataApiFactory _factory;

    public DataApiIntegrationTests(DataApiFactory factory)
    {
        _factory = factory;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class ListResponseDto
    {
        public List<FeedbackDto> Feedbacks { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private sealed class FeedbackDto
    {
        public string Id { get; set; } = "";
        public string Service { get; set; } = "";
        public string ServiceId { get; set; } = "";
        public double Rating { get; set; }
        public string? Text { get; set; }
        public double? Tonality { get; set; }
        public int? Status { get; set; }
        public string? AnswerSender { get; set; }
        public string? AnswerText { get; set; }
    }

    [Fact]
    public async Task Healthz_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("healthy", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task AddNew_ThenList_RoundTripPersistsFeedbackWithCompositeId()
    {
        var client = _factory.CreateClient();

        var payload = new
        {
            Feedbacks = new[]
            {
                new
                {
                    Id = "ignored",
                    Service = "wb",
                    ServiceId = "round-trip-77",
                    Rating = 0.8,
                    Text = "integration text",
                    CreatedDate = DateTime.Parse("2024-06-01T12:00:00Z").ToUniversalTime()
                }
            }
        };

        var addResponse = await client.PostAsJsonAsync("/api/feedbacks/add-new", payload);

        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var dateFrom = DateTimeOffset.Parse("2024-06-01T00:00:00Z").ToUnixTimeSeconds();
        var dateTo = DateTimeOffset.Parse("2024-06-02T00:00:00Z").ToUnixTimeSeconds();

        var listResponse = await client.GetAsync(
            $"/api/feedbacks/list?dateFrom={dateFrom}&dateTo={dateTo}&page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var data = await listResponse.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);

        Assert.NotNull(data);
        Assert.Equal(1, data!.Total);
        var item = Assert.Single(data.Feedbacks);
        Assert.Equal("wb:round-trip-77", item.Id);
        Assert.Equal("wb", item.Service);
        Assert.Equal("round-trip-77", item.ServiceId);
        Assert.Equal(0.8, item.Rating);
        Assert.Equal("integration text", item.Text);
    }

    [Fact]
    public async Task List_MissingDateParams_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/feedbacks/list");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("dateFrom", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_PageLessThanOne_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/feedbacks/list?dateFrom=1&dateTo=2&page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("page", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task List_PageSizeIsClampedTo200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/feedbacks/list?dateFrom=1&dateTo=2&page=1&pageSize=100000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);
        Assert.Equal(200, data!.PageSize);
    }

    [Fact]
    public async Task List_ArchivedFilterWithoutArchivedItems_ReturnsEmptyResult()
    {
        var client = _factory.CreateClient();

        // Уникальное окно дат — изоляция от других тестов на общей базе фабрики.
        var addResponse = await client.PostAsJsonAsync("/api/feedbacks/add-new", new
        {
            Feedbacks = new[]
            {
                new
                {
                    Service = "wb",
                    ServiceId = "no-status-row-it",
                    Rating = 0.5,
                    CreatedDate = DateTime.Parse("2049-01-01T00:00:00Z").ToUniversalTime()
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        var dateFrom = DateTimeOffset.Parse("2049-01-01T00:00:00Z").ToUnixTimeSeconds();
        var dateTo = DateTimeOffset.Parse("2049-01-02T00:00:00Z").ToUnixTimeSeconds();

        var archivedFiltered = await client.GetAsync($"/api/feedbacks/list?dateFrom={dateFrom}&dateTo={dateTo}&status={(int)FeedbackAnswerStatuses.Archived}");
        var all = await client.GetAsync($"/api/feedbacks/list?dateFrom={dateFrom}&dateTo={dateTo}");

        var filteredData = await archivedFiltered.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);
        var allData = await all.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);

        Assert.Equal(0, filteredData!.Total);
        Assert.Contains(allData!.Feedbacks, f => f.Id == "wb:no-status-row-it");
    }

    [Fact]
    public async Task FullFlow_ClassificationPriorityAnswerAndArchive()
    {
        var client = _factory.CreateClient();

        var addResponse = await client.PostAsJsonAsync("/api/feedbacks/add-new", new
        {
            Feedbacks = new[]
            {
                new
                {
                    Service = "wb",
                    ServiceId = "flow-neg",
                    Rating = 1.0,
                    Text = "Ужасный товар, сломался в первый день!",
                    CreatedDate = DateTime.Parse("2050-07-01T10:00:00Z").ToUniversalTime()
                },
                new
                {
                    Service = "wb",
                    ServiceId = "flow-pos",
                    Rating = 5.0,
                    Text = "Отличный товар, всё нравится!",
                    CreatedDate = DateTime.Parse("2050-07-02T10:00:00Z").ToUniversalTime()
                },
                new
                {
                    Service = "wb",
                    ServiceId = "flow-neu",
                    Rating = 3.0,
                    Text = "Обычный товар без изысков.",
                    CreatedDate = DateTime.Parse("2050-07-03T10:00:00Z").ToUniversalTime()
                }
            }
        });
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

        // Уникальное окно дат — изоляция от других тестов на общей базе фабрики.
        var dateFrom = DateTimeOffset.Parse("2050-07-01T00:00:00Z").ToUnixTimeSeconds();
        var dateTo = DateTimeOffset.Parse("2050-07-04T00:00:00Z").ToUnixTimeSeconds();

        // Приоритетные — только негативный отзыв.
        var priority = await client.GetAsync($"/api/feedbacks/list?dateFrom={dateFrom}&dateTo={dateTo}&priority=true");
        Assert.Equal(HttpStatusCode.OK, priority.StatusCode);

        var priorityData = await priority.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);
        Assert.NotNull(priorityData);
        Assert.Equal(1, priorityData!.Total);

        var negative = Assert.Single(priorityData.Feedbacks);
        Assert.Equal("wb:flow-neg", negative.Id);
        Assert.True(negative.Tonality < 0);
        Assert.Equal((int)(FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.NotHandled), negative.Status);
        Assert.Null(negative.AnswerText);

        // Ответ оператора на негативный отзыв.
        var answer = await client.PostAsJsonAsync("/api/feedbacks/answer", new
        {
            FeedbackId = "wb:flow-neg",
            Sender = "operator",
            Text = "Приносим извинения за неудобства."
        });
        Assert.Equal(HttpStatusCode.OK, answer.StatusCode);

        var requireList = await client.GetAsync(
            $"/api/feedbacks/list?dateFrom={dateFrom}&dateTo={dateTo}&status={(int)FeedbackAnswerStatuses.RequireToAnswer}");
        var requireData = await requireList.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);
        Assert.NotNull(requireData);
        Assert.Equal(2, requireData!.Total);
        Assert.DoesNotContain(requireData.Feedbacks, f => f.Id == "wb:flow-neg");

        var answeredList = await client.GetAsync(
            $"/api/feedbacks/list?dateFrom={dateFrom}&dateTo={dateTo}&status={(int)FeedbackAnswerStatuses.Answered}");
        var answeredData = await answeredList.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);
        Assert.NotNull(answeredList);

        var answered = Assert.Single(answeredData!.Feedbacks);
        Assert.Equal("wb:flow-neg", answered.Id);
        Assert.Equal("operator", answered.AnswerSender);
        Assert.Equal("Приносим извинения за неудобства.", answered.AnswerText);

        // Архивация позитивного отзыва.
        var archive = await client.PostAsync("/api/feedbacks/archive/wb:flow-pos", content: null);
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);

        var archivedList = await client.GetAsync(
            $"/api/feedbacks/list?dateFrom={dateFrom}&dateTo={dateTo}&status={(int)FeedbackAnswerStatuses.Archived}");
        var archivedData = await archivedList.Content.ReadFromJsonAsync<ListResponseDto>(JsonOptions);

        Assert.Equal("wb:flow-pos", Assert.Single(archivedData!.Feedbacks).Id);

        // Несуществующий отзыв → NotFound.
        var missingAnswer = await client.PostAsJsonAsync("/api/feedbacks/answer", new
        {
            FeedbackId = "wb:missing",
            Sender = "operator",
            Text = "ответ"
        });
        Assert.Equal(HttpStatusCode.NotFound, missingAnswer.StatusCode);

        var missingArchive = await client.PostAsync("/api/feedbacks/archive/wb:missing", content: null);
        Assert.Equal(HttpStatusCode.NotFound, missingArchive.StatusCode);
    }
}
