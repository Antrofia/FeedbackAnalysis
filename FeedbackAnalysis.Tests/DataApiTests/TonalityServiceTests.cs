using System.Globalization;
using System.Net;
using System.Text;
using FeedbackAnalysis.DataApi.Services;
using FeedbackAnalysis.Tests.TestSupport;
using Microsoft.Extensions.Logging;

namespace FeedbackAnalysis.Tests.DataApiTests;

/// <summary>
/// Тесты TonalityService: маппинг label (0=NEUTRAL, 1=POSITIVE, 2=NEGATIVE)
/// и устойчивость к ошибкам ML-сервиса (всегда null вместо исключения).
/// </summary>
public class TonalityServiceTests
{
    private const string BaseAddress = "http://ml-stub/";

    private static (TonalityService Service, StubHttpMessageHandler Handler, CollectingLogger<TonalityService> Logger) Create(
        Func<HttpRequestMessage, HttpResponseMessage>? responder = null)
    {
        var handler = new StubHttpMessageHandler(responder);
        var logger = new CollectingLogger<TonalityService>();
        var service = new TonalityService(new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) }, logger);

        return (service, handler, logger);
    }

    private static HttpResponseMessage Json(HttpStatusCode code, string body)
    {
        return new HttpResponseMessage(code)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }

    [Theory]
    [InlineData(2, 0.97, -0.97)]
    [InlineData(1, 0.95, 0.95)]
    [InlineData(0, 0.9, 0.0)]
    public async Task ClassifyAsync_MapsLabelToSignedConfidence(int label, double confidence, double expected)
    {
        var body = $"{{\"text\":\"текст\",\"label\":{label},\"confidence\":{confidence.ToString(CultureInfo.InvariantCulture)}}}";
        var (service, handler, _) = Create(_ => Json(HttpStatusCode.OK, body));

        var result = await service.ClassifyAsync("некий текст");

        Assert.Equal(expected, result);
        Assert.Equal($"{BaseAddress}predict", Assert.Single(handler.RequestUris));
        Assert.Equal("{\"text\":\"некий текст\"}", Assert.Single(handler.RequestBodies));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ClassifyAsync_EmptyText_ReturnsNullWithoutRequest(string? text)
    {
        var (service, handler, _) = Create();

        Assert.Null(await service.ClassifyAsync(text));
        Assert.Empty(handler.RequestUris);
    }

    [Fact]
    public async Task ClassifyAsync_Non2xxResponse_LogsErrorAndReturnsNull()
    {
        var (service, _, logger) = Create(_ => Json(HttpStatusCode.InternalServerError, "boom"));

        Assert.Null(await service.ClassifyAsync("текст"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("не-2xx"));
    }

    [Fact]
    public async Task ClassifyAsync_InvalidJson_LogsErrorAndReturnsNull()
    {
        var (service, _, logger) = Create(_ => Json(HttpStatusCode.OK, "not a json"));

        Assert.Null(await service.ClassifyAsync("текст"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ClassifyAsync_MissingFields_LogsErrorAndReturnsNull()
    {
        var (service, _, logger) = Create(_ => Json(HttpStatusCode.OK, "{\"text\":\"текст\",\"label\":1}"));

        Assert.Null(await service.ClassifyAsync("текст"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ClassifyAsync_UnknownLabel_LogsErrorAndReturnsNull()
    {
        var (service, _, logger) = Create(_ => Json(HttpStatusCode.OK, "{\"text\":\"текст\",\"label\":5,\"confidence\":0.5}"));

        Assert.Null(await service.ClassifyAsync("текст"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("label"));
    }

    [Fact]
    public async Task ClassifyAsync_UnreachableService_LogsErrorAndReturnsNull()
    {
        // Handler без BaseAddress → относительный URI бросает InvalidOperationException.
        var logger = new CollectingLogger<TonalityService>();
        var service = new TonalityService(new HttpClient(new StubHttpMessageHandler()), logger);

        Assert.Null(await service.ClassifyAsync("текст"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ClassifyBatchAsync_MultipleTexts_SingleRequest_MappedInOrder()
    {
        const string responseBody = """
            {"results":[
                {"text":"плохой","label":2,"confidence":0.97},
                {"text":"отличный","label":1,"confidence":0.95},
                {"text":"нормальный","label":0,"confidence":0.9}
            ]}
            """;
        var (service, handler, _) = Create(_ => Json(HttpStatusCode.OK, responseBody));

        var result = await service.ClassifyBatchAsync(["плохой", "отличный", "нормальный"]);

        Assert.Equal([-0.97, 0.95, 0.0], result);
        var uri = Assert.Single(handler.RequestUris);
        Assert.EndsWith("/predict/batch", uri);
        Assert.Equal("{\"texts\":[\"плохой\",\"отличный\",\"нормальный\"]}", Assert.Single(handler.RequestBodies));
    }

    [Fact]
    public async Task ClassifyBatchAsync_SingleText_UsesSingleEndpoint()
    {
        var (service, handler, _) = Create(_ => Json(HttpStatusCode.OK, "{\"text\":\"текст\",\"label\":1,\"confidence\":0.8}"));

        var result = await service.ClassifyBatchAsync(["текст"]);

        Assert.Equal([0.8], result);
        Assert.Equal($"{BaseAddress}predict", Assert.Single(handler.RequestUris));
    }

    [Fact]
    public async Task ClassifyBatchAsync_EmptyEntries_SingleNonEmptyGoesToSingleEndpoint_NullsInResult()
    {
        var (service, handler, _) = Create(_ => Json(HttpStatusCode.OK, "{\"text\":\"хорошо\",\"label\":1,\"confidence\":0.7}"));

        var result = await service.ClassifyBatchAsync(["", null, "  ", "хорошо"]);

        Assert.Equal(new double?[] { null, null, null, 0.7 }, result);
        Assert.Equal($"{BaseAddress}predict", Assert.Single(handler.RequestUris));
        Assert.Equal("{\"text\":\"хорошо\"}", Assert.Single(handler.RequestBodies));
    }

    [Fact]
    public async Task ClassifyBatchAsync_AllEmptyEntries_NoRequest_ReturnsNullsByInputLength()
    {
        var (service, handler, _) = Create();

        var result = await service.ClassifyBatchAsync(["", "   "]);

        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Null(x));
        Assert.Empty(handler.RequestUris);
    }

    [Fact]
    public async Task ClassifyBatchAsync_Non2xxResponse_AllNullsAndErrorLog()
    {
        var (service, handler, logger) = Create(_ => Json(HttpStatusCode.ServiceUnavailable, "boom"));

        var result = await service.ClassifyBatchAsync(["а", "б"]);

        Assert.Equal([null, null], result);
        Assert.Single(handler.RequestUris);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("не-2xx"));
    }

    [Fact]
    public async Task ClassifyBatchAsync_ResponseLengthMismatch_AllNullsAndErrorLog()
    {
        var (service, _, logger) = Create(_ => Json(HttpStatusCode.OK, "{\"results\":[{\"text\":\"а\",\"label\":1,\"confidence\":0.5}]}"));

        var result = await service.ClassifyBatchAsync(["а", "б"]);

        Assert.Equal([null, null], result);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("результат"));
    }
}
