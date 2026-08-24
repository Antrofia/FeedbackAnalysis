using FeedbackAnalysis.FakeFeedbacksService.Services;
using Microsoft.Extensions.Configuration;

namespace FeedbackAnalysis.FakeFeedbacksService.BackgroundWork
{
    /// <summary>
    /// Фоновая генерация фейковых отзывов по таймеру.
    /// Включается одним ключом конфига/env-переменной Generator:TimerEnabled=true.
    /// </summary>
    public class GenerationTimerService : BackgroundService
    {
        private readonly IFakeFeedbacksGenerator _generator;
        private readonly IFeedbacksPublisher _publisher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GenerationTimerService> _logger;

        public GenerationTimerService(
            IFakeFeedbacksGenerator generator,
            IFeedbacksPublisher publisher,
            IConfiguration configuration,
            ILogger<GenerationTimerService> logger)
        {
            _generator = generator;
            _publisher = publisher;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var generatorSection = _configuration.GetSection("Generator");

            if (!generatorSection.GetValue<bool>("TimerEnabled"))
            {
                _logger.LogInformation("Генерация фейковых отзывов отключена (Generator:TimerEnabled=false)");
                return;
            }

            var batchSize = generatorSection.GetValue("BatchSize", 10);
            var interval = TimeSpan.FromSeconds(generatorSection.GetValue("IntervalSeconds", 60));

            _logger.LogInformation(
                "Таймер генерации запущен: батч из {batchSize} отзывов каждые {seconds} c",
                batchSize,
                interval.TotalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var batch = _generator.GenerateBatch(batchSize);

                    await _publisher.PublishAsync(batch, stoppingToken);

                    _logger.LogInformation("Сгенерирован и отправлен батч из {count} фейковых отзывов", batch.Count);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Публикация свои ошибки уже погасила; страхуемся от прочих сбоев, чтобы цикл не умер
                    _logger.LogError(ex, "Сбой при генерации/публикации батча фейковых отзывов");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("Таймер генерации остановлен");
        }
    }
}
