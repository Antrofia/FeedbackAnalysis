using System.Net;
using System.Text.Json;
using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.FakeFeedbacksService.Services;
using FeedbackAnalysis.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FeedbackAnalysis.Tests.FakeFeedbacksServiceTests;

public class PublisherTests
{
    private const string DataApiHost = "http://fake-data-api";

    private static (FeedbacksPublisher Publisher, StubHttpMessageHandler Stub, CollectingLogger<IFeedbacksPublisher> Logger)
        CreatePublisher(Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Services:FeedbacksData"] = DataApiHost })
            .Build();

        var stub = new StubHttpMessageHandler(responder);
        var logger = new CollectingLogger<IFeedbacksPublisher>();
        var publisher = new FeedbacksPublisher(new HttpClient(stub), configuration, logger);

        return (publisher, stub, logger);
    }

    private static List<FeedbackModel> SampleBatch(int count) => Enumerable.Range(1, count)
        .Select(i => new FeedbackModel
        {
            Service = "fake",
            ServiceId = $"sid-{i}",
            Rating = 4.5,
            Sender = "Тестовый Отправитель",
            Text = "Текст отзыва",
            CreatedDate = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            NomenclatureLink = "https://www.wildberries.ru/catalog/123/detail.aspx"
        })
        .ToList();

    [Fact]
    public async Task Publish_SendsPostToConfiguredHostAndPath()
    {
        var (publisher, stub, _) = CreatePublisher();

        await publisher.PublishAsync(SampleBatch(3));

        Assert.Equal($"{DataApiHost}/api/feedbacks/add-new", Assert.Single(stub.RequestUris));
    }

    [Fact]
    public async Task Publish_BodyContainsCamelCaseFeedbacksArray()
    {
        var (publisher, stub, _) = CreatePublisher();

        await publisher.PublishAsync(SampleBatch(3));

        using var doc = JsonDocument.Parse(stub.RequestBodies.Single()!);

        var feedbacks = doc.RootElement.GetProperty("feedbacks");
        Assert.Equal(3, feedbacks.GetArrayLength());

        var first = feedbacks[0];
        Assert.Equal("fake", first.GetProperty("service").GetString());
        Assert.Equal("sid-1", first.GetProperty("serviceId").GetString());
        Assert.Equal(4.5, first.GetProperty("rating").GetDouble());
        Assert.True(first.TryGetProperty("createdDate", out _));
        Assert.True(first.TryGetProperty("nomenclatureLink", out _));
    }

    [Fact]
    public async Task Publish_Http500_LogsErrorWithoutThrowing()
    {
        var (publisher, _, logger) = CreatePublisher(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var exception = await Record.ExceptionAsync(() => publisher.PublishAsync(SampleBatch(2)));

        Assert.Null(exception);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task Publish_MissingDataApiHostConfig_LogsErrorAndSkipsRequest()
    {
        var configuration = new ConfigurationBuilder().Build();
        var stub = new StubHttpMessageHandler();
        var logger = new CollectingLogger<IFeedbacksPublisher>();
        var publisher = new FeedbacksPublisher(new HttpClient(stub), configuration, logger);

        var exception = await Record.ExceptionAsync(() => publisher.PublishAsync(SampleBatch(2)));

        Assert.Null(exception);
        Assert.Empty(stub.RequestUris);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }
}
