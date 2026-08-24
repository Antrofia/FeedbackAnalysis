using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.FakeFeedbacksService.Data;
using FeedbackAnalysis.FakeFeedbacksService.Services;
using Microsoft.Extensions.Configuration;

namespace FeedbackAnalysis.Tests.FakeFeedbacksServiceTests;

public class GeneratorTests
{
    private static IFakeFeedbacksGenerator CreateGenerator(Random? random = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Source:Name"] = "fake" })
            .Build();

        return new FakeFeedbacksGenerator(configuration, random ?? new Random());
    }

    private static bool IsIn(string[] texts, string? text) => text is not null && Array.IndexOf(texts, text) >= 0;

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(57)]
    public void GenerateBatch_ReturnsRequestedNumberOfFeedbacks(int size)
    {
        var batch = CreateGenerator().GenerateBatch(size);

        Assert.Equal(size, batch.Count);
    }

    [Fact]
    public void GenerateBatch_ServiceIdsAreUniqueOverLargeBatch()
    {
        var batch = CreateGenerator().GenerateBatch(200);

        Assert.Equal(batch.Count, batch.Select(f => f.ServiceId).Distinct().Count());
    }

    [Fact]
    public void GenerateBatch_RatingMatchesTonalityGroup()
    {
        // Эвристика: текст однозначно определяет группу, значит рейтинг
        // негативных должен быть <= 2, позитивных >= 4, нейтральных 3..4.
        var batch = CreateGenerator().GenerateBatch(300);

        foreach (var feedback in batch)
        {
            var inNegative = IsIn(FakeFeedbacksData.NegativeTexts, feedback.Text);
            var inPositive = IsIn(FakeFeedbacksData.PositiveTexts, feedback.Text);
            var inNeutral = IsIn(FakeFeedbacksData.NeutralTexts, feedback.Text);

            Assert.True(inNegative ^ inPositive ^ inNeutral, $"Текст не найден в заготовках: {feedback.Text}");

            if (inNegative)
            {
                Assert.InRange(feedback.Rating, 1.0, 2.0);
            }
            else if (inPositive)
            {
                Assert.InRange(feedback.Rating, 4.0, 5.0);
            }
            else
            {
                Assert.InRange(feedback.Rating, 3.0, 4.0);
            }
        }
    }

    [Fact]
    public void GenerateBatch_TextAndSenderAreNotEmpty()
    {
        var batch = CreateGenerator().GenerateBatch(100);

        Assert.All(batch, feedback =>
        {
            Assert.False(string.IsNullOrWhiteSpace(feedback.Text));
            Assert.False(string.IsNullOrWhiteSpace(feedback.Sender));
        });
    }

    [Fact]
    public void GenerateBatch_CreatedDateIsWithinLastMonthWindow()
    {
        var beforeGeneration = DateTime.UtcNow;
        var batch = CreateGenerator().GenerateBatch(150);
        var afterGeneration = DateTime.UtcNow;

        Assert.All(batch, feedback =>
        {
            Assert.NotNull(feedback.CreatedDate);
            Assert.InRange(feedback.CreatedDate.Value.Ticks, beforeGeneration.AddDays(-31).Ticks, afterGeneration.Ticks);
        });
    }

    [Fact]
    public void GenerateBatch_ServiceComesFromSourceNameConfig()
    {
        var batch = CreateGenerator().GenerateBatch(20);

        Assert.All(batch, feedback => Assert.Equal("fake", feedback.Service));
    }

    [Fact]
    public void GenerateBatch_NomenclatureLinkUsesProductPoolFormat()
    {
        var batch = CreateGenerator().GenerateBatch(50);

        Assert.All(batch, feedback =>
        {
            Assert.NotNull(feedback.NomenclatureLink);
            Assert.StartsWith("https://www.wildberries.ru/catalog/", feedback.NomenclatureLink);
            Assert.EndsWith("/detail.aspx", feedback.NomenclatureLink);

            var productId = feedback.NomenclatureLink!["https://www.wildberries.ru/catalog/".Length..^"/detail.aspx".Length];
            Assert.Contains(productId, FakeFeedbacksData.ProductIds);
        });
    }
}
