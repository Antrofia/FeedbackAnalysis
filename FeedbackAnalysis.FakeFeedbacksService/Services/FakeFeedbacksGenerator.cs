using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.FakeFeedbacksService.Data;
using Microsoft.Extensions.Configuration;

namespace FeedbackAnalysis.FakeFeedbacksService.Services
{
    public class FakeFeedbacksGenerator : IFakeFeedbacksGenerator
    {
        private const string GeneratorSectionName = "Generator";

        // Значения по умолчанию при отсутствии/некорректности конфига: примерно 40% / 30% / 30%
        public const int DefaultPositiveWeight = 40;
        public const int DefaultNeutralWeight = 30;
        public const int DefaultNegativeWeight = 30;

        private readonly IConfiguration _configuration;
        private readonly Random _random;

        // Относительные веса тональностей в батче (из Generator:Weights:{Positive|Neutral|Negative})
        private readonly int _positiveWeight;
        private readonly int _neutralWeight;
        private readonly int _negativeWeight;

        public FakeFeedbacksGenerator(IConfiguration configuration, Random? random = null)
        {
            _configuration = configuration;
            _random = random ?? new Random();

            var generatorSection = configuration.GetSection(GeneratorSectionName);

            _positiveWeight = Math.Max(0, generatorSection.GetValue("Weights:Positive", DefaultPositiveWeight));
            _neutralWeight = Math.Max(0, generatorSection.GetValue("Weights:Neutral", DefaultNeutralWeight));
            _negativeWeight = Math.Max(0, generatorSection.GetValue("Weights:Negative", DefaultNegativeWeight));

            // Страховка от вырожденной конфигурации (все веса нулевые) — используем значения по умолчанию
            if (_positiveWeight + _neutralWeight + _negativeWeight == 0)
            {
                _positiveWeight = DefaultPositiveWeight;
                _neutralWeight = DefaultNeutralWeight;
                _negativeWeight = DefaultNegativeWeight;
            }
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
            // Взвешенный выбор: roll попадает в один из диапазонов [0, positive), [positive, positive+neutral), ...
            var roll = _random.Next(_positiveWeight + _neutralWeight + _negativeWeight);

            if (roll < _positiveWeight)
            {
                return FeedbackTonality.Positive;
            }

            return roll < _positiveWeight + _neutralWeight ? FeedbackTonality.Neutral : FeedbackTonality.Negative;
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
