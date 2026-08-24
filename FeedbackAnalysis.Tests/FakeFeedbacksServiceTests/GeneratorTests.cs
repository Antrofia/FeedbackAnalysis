using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.FakeFeedbacksService.Data;
using FeedbackAnalysis.FakeFeedbacksService.Services;
using Microsoft.Extensions.Configuration;

namespace FeedbackAnalysis.Tests.FakeFeedbacksServiceTests;

public class GeneratorTests
{
    private static IFakeFeedbacksGenerator CreateGenerator(Random? random = null, Dictionary<string, string?>? config = null)
    {
        var settings = new Dictionary<string, string?> { ["Source:Name"] = "fake" };
        if (config is not null)
        {
            foreach (var (key, value) in config)
            {
                settings[key] = value;
            }
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new FakeFeedbacksGenerator(configuration, random ?? new Random());
    }

    private static Dictionary<string, string?> WeightsConfig(int positive, int neutral, int negative) => new()
    {
        ["Generator:Weights:Positive"] = positive.ToString(),
        ["Generator:Weights:Neutral"] = neutral.ToString(),
        ["Generator:Weights:Negative"] = negative.ToString()
    };

    private static bool IsIn(string[] texts, string? text) => text is not null && Array.IndexOf(texts, text) >= 0;

    private static bool IsPositive(string? text) => IsIn(FakeFeedbacksData.PositiveTexts, text);
    private static bool IsNeutral(string? text) => IsIn(FakeFeedbacksData.NeutralTexts, text);
    private static bool IsNegative(string? text) => IsIn(FakeFeedbacksData.NegativeTexts, text);

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

    [Theory]
    [InlineData(100, 0, 0)]
    [InlineData(0, 100, 0)]
    [InlineData(0, 0, 100)]
    public void GenerateBatch_WeightsComeFromConfig(int positive, int neutral, int negative)
    {
        var batch = CreateGenerator(config: WeightsConfig(positive, neutral, negative)).GenerateBatch(30);

        Assert.NotEmpty(batch);

        foreach (var feedback in batch)
        {
            if (positive == 100)
            {
                Assert.True(IsPositive(feedback.Text), $"Ожидался позитивный текст: {feedback.Text}");
            }
            else if (neutral == 100)
            {
                Assert.True(IsNeutral(feedback.Text), $"Ожидался нейтральный текст: {feedback.Text}");
            }
            else
            {
                Assert.True(IsNegative(feedback.Text), $"Ожидался негативный текст: {feedback.Text}");
            }
        }
    }

    [Fact]
    public void GenerateBatch_WeightRatioFollowsConfig()
    {
        // Соотношение 2:1:1 — при фиксированном Random детерминированно;
        // проверяем статистически на большой выборке
        var batch = CreateGenerator(config: WeightsConfig(60, 20, 20)).GenerateBatch(400);

        var positive = batch.Count(f => IsPositive(f.Text));
        var neutral = batch.Count(f => IsNeutral(f.Text));
        var negative = batch.Count(f => IsNegative(f.Text));

        // Допуск ±10 п.п. от ожидаемых долей 60/20/20%
        Assert.InRange(positive, 200, 280);
        Assert.InRange(neutral + negative, 120, 200);
        Assert.Equal(batch.Count, positive + neutral + negative);
    }

    [Fact]
    public void GenerateBatch_AllZeroWeightsFallBackToDefaults()
    {
        var batch = CreateGenerator(config: WeightsConfig(0, 0, 0)).GenerateBatch(300);

        // При вырожденной конфигурации используются значения по умолчанию —
        // встречаются все три группы тональности
        Assert.Contains(batch, f => IsPositive(f.Text));
        Assert.Contains(batch, f => IsNeutral(f.Text));
        Assert.Contains(batch, f => IsNegative(f.Text));
    }

    [Fact]
    public void GenerateBatch_NegativeWeightsAreClampedToZero()
    {
        var batch = CreateGenerator(config: WeightsConfig(-5, 50, 50)).GenerateBatch(200);

        Assert.DoesNotContain(batch, f => IsPositive(f.Text));
    }
}
