using System.Net;
using System.Text.Json;
using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.FakeFeedbacksService.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FeedbackAnalysis.Tests.FakeFeedbacksServiceTests;

/// <summary>
/// Фабрика FakeFeedbacksService с подменённым публикатором на Moq-заглушку.
/// </summary>
public sealed class FakeFeedbacksFactory : WebApplicationFactory<FeedbackAnalysis.FakeFeedbacksService.Program>
{
    public Mock<IFeedbacksPublisher> PublisherMock { get; } = new(MockBehavior.Strict);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFeedbacksPublisher>();
            services.AddSingleton(_ => PublisherMock.Object);
        });
    }
}

public class GenerationControllerIntegrationTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly FakeFeedbacksFactory _factory = new();

    private void SetupPublisherCapture(List<List<FeedbackModel>> capturedBatches)
    {
        _factory.PublisherMock
            .Setup(p => p.PublishAsync(Capture.In(capturedBatches), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public void Dispose() => _factory.Dispose();

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
    public async Task Run_WithCount_GeneratesAndPublishesSingleBatchOfSizeCount()
    {
        var captured = new List<List<FeedbackModel>>();
        SetupPublisherCapture(captured);

        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/generation/run?count=5", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(5, doc.RootElement.GetProperty("generated").GetInt32());

        _factory.PublisherMock.Verify(
            p => p.PublishAsync(It.IsAny<List<FeedbackModel>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        var batch = Assert.Single(captured);
        Assert.Equal(5, batch.Count);
        Assert.All(batch, feedback =>
        {
            Assert.Equal("fake", feedback.Service);
            Assert.False(string.IsNullOrWhiteSpace(feedback.ServiceId));
        });
    }

    [Fact]
    public async Task Run_WithoutCount_UsesConfiguredBatchSize()
    {
        var captured = new List<List<FeedbackModel>>();
        SetupPublisherCapture(captured);

        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/generation/run", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(10, doc.RootElement.GetProperty("generated").GetInt32());

        var batch = Assert.Single(captured);
        Assert.Equal(10, batch.Count);
    }

    [Fact]
    public async Task Run_CountIsClampedToAllowedRange()
    {
        var captured = new List<List<FeedbackModel>>();
        SetupPublisherCapture(captured);

        var client = _factory.CreateClient();

        var tooBig = await client.PostAsync("/api/generation/run?count=100000", content: null);
        var tooSmall = await client.PostAsync("/api/generation/run?count=0", content: null);

        Assert.Equal(HttpStatusCode.OK, tooBig.StatusCode);
        using (var doc = JsonDocument.Parse(await tooBig.Content.ReadAsStringAsync()))
        {
            Assert.Equal(200, doc.RootElement.GetProperty("generated").GetInt32());
        }

        Assert.Equal(HttpStatusCode.OK, tooSmall.StatusCode);
        using (var doc = JsonDocument.Parse(await tooSmall.Content.ReadAsStringAsync()))
        {
            Assert.Equal(1, doc.RootElement.GetProperty("generated").GetInt32());
        }
    }
}
