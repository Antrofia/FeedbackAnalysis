using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.FakeFeedbacksService.Data;
using Microsoft.Extensions.Configuration;

namespace FeedbackAnalysis.FakeFeedbacksService.Services
{
    public class FakeFeedbacksGenerator : IFakeFeedbacksGenerator
    {
        // Распределение тональности в батче: примерно 40% / 30% / 30%
        private const int PositiveSharePercent = 40;
        private const int NeutralSharePercent = 70; // верхняя граница positive+neutral

        private readonly IConfiguration _configuration;
        private readonly Random _random;

        public FakeFeedbacksGenerator(IConfiguration configuration, Random? random = null)
        {
            _configuration = configuration;
            _random = random ?? new Random();
        }

        public List<FeedbackModel> GenerateBatch(int count)
        {
            var serviceName = _configuration.GetSection("Source").GetValue("Name", "fake");

            var batch = new List<FeedbackModel>(count);

            for (var i = 0; i < count; i++)
            {
                var tonality = NextTonality();

                batch.Add(new FeedbackModel
                {
                    Service = serviceName,
                    ServiceId = Guid.NewGuid().ToString(),
                    Rating = NextRating(tonality),
                    Sender = Pick(FakeFeedbacksData.Senders),
                    Text = Pick(TextsOf(tonality)),
                    CreatedDate = DateTime.UtcNow
                        .AddDays(-_random.Next(0, 31))
                        .AddMinutes(-_random.Next(0, 60)),
                    NomenclatureLink = $"https://www.wildberries.ru/catalog/{Pick(FakeFeedbacksData.ProductIds)}/detail.aspx"
                });
            }

            return batch;
        }

        private FeedbackTonality NextTonality()
        {
            var roll = _random.Next(100);

            if (roll < PositiveSharePercent)
            {
                return FeedbackTonality.Positive;
            }

            return roll < NeutralSharePercent ? FeedbackTonality.Neutral : FeedbackTonality.Negative;
        }

        private double NextRating(FeedbackTonality tonality)
        {
            // Рейтинг согласован с тональностью: positive → 4..5, neutral → 3..4, negative → 1..2
            var (min, max) = tonality switch
            {
                FeedbackTonality.Positive => (4.0, 5.0),
                FeedbackTonality.Neutral => (3.0, 4.0),
                _ => (1.0, 2.0)
            };

            // Шаг 0.5, чтобы рейтинг выглядел правдоподобно (например, 4.5)
            return Math.Round((min + _random.NextDouble() * (max - min)) * 2, MidpointRounding.AwayFromZero) / 2;
        }

        private static string[] TextsOf(FeedbackTonality tonality) => tonality switch
        {
            FeedbackTonality.Positive => FakeFeedbacksData.PositiveTexts,
            FeedbackTonality.Neutral => FakeFeedbacksData.NeutralTexts,
            _ => FakeFeedbacksData.NegativeTexts
        };

        private string Pick(string[] source) => source[_random.Next(source.Length)];
    }
}
