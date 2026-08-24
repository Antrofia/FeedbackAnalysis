using System.Net;
using System.Text;
using AutoMapper;
using FeedbackAnalysis.ClientUI.Models;
using FeedbackAnalysis.ClientUI.Services;
using FeedbackAnalysis.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FeedbackAnalysis.Tests.ClientUITests;

public class ClientUiFeedbacksServiceTests
{
    private const long DateFromUnix = 1704067200; // 2024-01-01T00:00:00Z
    private const long DateToUnix = 1706745600;   // 2024-02-01T00:00:00Z

    private static readonly DateTime DateFrom = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DateTo = new(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetFeedbacks_BuildsListUrlWithStatusPriorityPagePageSize()
    {
        var handler = new StubHttpMessageHandler();
        var sut = CreateSut("http://dataapi.test", handler);

        await sut.GetFeedbacksAsync(DateFrom, DateTo, status: 1, priority: true, page: 2, pageSize: 10);

        Assert.Equal(
            "http://dataapi.test/api/feedbacks/list?dateFrom=1704067200&dateTo=1706745600&status=1&priority=true&page=2&pageSize=10",
            Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task GetFeedbacks_UnsuccessfulResponse_ReturnsEmptyPageAndLogsError()
    {
        var logger = new CollectingLogger<IFeedbacksService>();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("overloaded")
        });
        var sut = CreateSut("http://dataapi.test", handler, logger);

        var result = await sut.GetFeedbacksAsync(DateFrom, DateTo, status: 1, priority: false);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("ServiceUnavailable"));
    }

    [Fact]
    public async Task GetFeedbacks_SuccessfulResponse_DeserializesIntoViewModels()
    {
        const string body = """
            {
              "feedbacks": [
                {
                  "id": "wb:33",
                  "service": "wb",
                  "serviceId": "33",
                  "rating": 4.0,
                  "sender": "Пётр",
                  "text": "супер",
                  "createdDate": "2024-01-15T12:00:00Z",
                  "nomenclatureLink": "www.wildberries.ru/catalog/9/detail.aspx",
                  "tonality": -0.5,
                  "status": 6,
                  "answerSender": "Оператор",
                  "answerText": "Спасибо за отзыв!"
                },
                {
                  "id": "wb:34",
                  "service": "wb",
                  "serviceId": "34",
                  "rating": 1.0,
                  "tonality": null,
                  "status": null
                }
              ],
              "total": 7,
              "page": 1,
              "pageSize": 20
            }
            """;

        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        var sut = CreateSut("http://dataapi.test", handler);

        var result = await sut.GetFeedbacksAsync(DateFrom, DateTo, status: 1, priority: false);

        Assert.Equal(7, result.Total);

        var feedback = result.Items[0];
        Assert.Equal("wb:33", feedback.Id);
        Assert.Equal(4.0, feedback.Rating, precision: 10);
        Assert.Equal("Пётр", feedback.Sender);
        Assert.Equal("супер", feedback.Text);
        Assert.Equal(DateTime.Parse("2024-01-15T12:00:00Z").ToUniversalTime(), feedback.CreatedDate!.Value.ToUniversalTime());
        Assert.Equal("www.wildberries.ru/catalog/9/detail.aspx", feedback.NomenclatureLink);
        Assert.Equal(-0.5, feedback.Tonality!.Value, precision: 10);
        Assert.Equal(6, feedback.Status);
        Assert.Equal("Оператор", feedback.AnswerSender);
        Assert.Equal("Спасибо за отзыв!", feedback.AnswerText);

        var withoutEnrichment = result.Items[1];
        Assert.Null(withoutEnrichment.Tonality);
        Assert.Null(withoutEnrichment.Status);
        Assert.Null(withoutEnrichment.AnswerSender);
        Assert.Null(withoutEnrichment.AnswerText);
    }

    [Fact]
    public async Task GetFeedbacks_EmptyFeedbacksInResponse_ReturnsEmptyItemsWithTotal()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"feedbacks": [], "total": 42}""", Encoding.UTF8, "application/json")
        });
        var sut = CreateSut("http://dataapi.test", handler);

        var result = await sut.GetFeedbacksAsync(DateFrom, DateTo, status: 1, priority: false);

        Assert.Empty(result.Items);
        Assert.Equal(42, result.Total);
    }

    [Fact]
    public async Task SendAnswer_PostsCamelCaseJsonToAnswerEndpoint_AndReturnsTrueOnSuccess()
    {
        HttpMethod? capturedMethod = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedMethod = request.Method;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CreateSut("http://dataapi.test", handler);

        var success = await sut.SendAnswerAsync("wb:33", "Администратор", "Спасибо за отзыв!");

        Assert.True(success);
        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal(
            "http://dataapi.test/api/feedbacks/answer",
            Assert.Single(handler.RequestUris));
        Assert.Equal(
            """{"feedbackId":"wb:33","sender":"Администратор","text":"Спасибо за отзыв!"}""",
            Assert.Single(handler.RequestBodies));
    }

    [Fact]
    public async Task SendAnswer_NonSuccessResponse_ReturnsFalseAndLogsError()
    {
        var logger = new CollectingLogger<IFeedbacksService>();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found")
        });
        var sut = CreateSut("http://dataapi.test", handler, logger);

        var success = await sut.SendAnswerAsync("wb:33", "Администратор", "Ответ");

        Assert.False(success);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("NotFound"));
    }

    [Fact]
    public async Task Archive_PostsToArchiveUrlWithId_AndReturnsTrueOnSuccess()
    {
        HttpMethod? capturedMethod = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedMethod = request.Method;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CreateSut("http://dataapi.test/", handler);

        var success = await sut.ArchiveAsync("wb-777");

        Assert.True(success);
        Assert.Equal(HttpMethod.Post, capturedMethod);
        Assert.Equal(
            "http://dataapi.test/api/feedbacks/archive/wb-777",
            Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task Archive_NonSuccessResponse_ReturnsFalseAndLogsError()
    {
        var logger = new CollectingLogger<IFeedbacksService>();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("not found")
        });
        var sut = CreateSut("http://dataapi.test", handler, logger);

        var success = await sut.ArchiveAsync("wb:missing");

        Assert.False(success);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("NotFound"));
    }

    private static FeedbacksService CreateSut(
        string dataApiUrl,
        StubHttpMessageHandler handler,
        CollectingLogger<IFeedbacksService>? logger = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Services:FeedbacksData"] = dataApiUrl
            })
            .Build();

        var mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

        return new FeedbacksService(
            new HttpClient(handler),
            configuration,
            mapperConfiguration.CreateMapper(),
            logger ?? new CollectingLogger<IFeedbacksService>());
    }
}
