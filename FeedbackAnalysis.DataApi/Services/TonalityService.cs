using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FeedbackAnalysis.DataApi.Services
{
    /// <summary>
    /// Клиент ML-сервиса (POST {Services:FeedbacksHandler}/predict).
    /// Любая ошибка логируется и возвращает null — приём отзывов не должен ломаться.
    /// </summary>
    public class TonalityService : ITonalityService
    {
        // Модель blanchefort/rubert-base-cased-sentiment-rurewiews: id2label = 0:NEUTRAL, 1:POSITIVE, 2:NEGATIVE.
        private const int LabelNeutral = 0;
        private const int LabelPositive = 1;
        private const int LabelNegative = 2;

        private readonly HttpClient _httpClient;
        private readonly ILogger<TonalityService> _logger;

        public TonalityService(HttpClient httpClient, ILogger<TonalityService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<double?> ClassifyAsync(string? text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                using var content = new StringContent(JsonConvert.SerializeObject(new { text }), Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync("predict", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ML-сервис вернул не-2xx ответ: {StatusCode}", (int)response.StatusCode);
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync(ct);

                return MapTonality(JsonConvert.DeserializeObject<PredictResponse>(body));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить тональность отзыва от ML-сервиса");
                return null;
            }
        }

        public async Task<IReadOnlyList<double?>> ClassifyBatchAsync(IReadOnlyCollection<string?> texts, CancellationToken ct = default)
        {
            var result = new double?[texts.Count];

            // В ML уходят только непустые тексты — результат мержится обратно по индексам.
            var indices = new List<int>(texts.Count);
            var nonEmptyTexts = new List<string>(texts.Count);

            var index = 0;
            foreach (var text in texts)
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    indices.Add(index);
                    nonEmptyTexts.Add(text);
                }

                index++;
            }

            if (indices.Count == 0)
            {
                return result;
            }

            IReadOnlyList<double?> classified = indices.Count == 1
                ? [await ClassifyAsync(nonEmptyTexts[0], ct)]
                : await ClassifyManyAsync(nonEmptyTexts, ct);

            for (var i = 0; i < indices.Count; i++)
            {
                result[indices[i]] = classified[i];
            }

            return result;
        }

        /// <summary>Батч-вызов /predict/batch: при любой ошибке — все null, приём отзывов не ломается.</summary>
        private async Task<IReadOnlyList<double?>> ClassifyManyAsync(List<string> texts, CancellationToken ct)
        {
            try
            {
                using var content = new StringContent(JsonConvert.SerializeObject(new { texts }), Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync("predict/batch", content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("ML-сервис вернул не-2xx ответ на батч-запрос: {StatusCode}", (int)response.StatusCode);
                    return NullResults(texts.Count);
                }

                var body = await response.Content.ReadAsStringAsync(ct);
                var parsed = JsonConvert.DeserializeObject<BatchPredictResponse>(body);

                if (parsed?.Results is not { Count: var count } || count != texts.Count)
                {
                    _logger.LogError(
                        "Невалидный батч-ответ ML-сервиса: ожидалось {Expected} результатов, получено {Actual}",
                        texts.Count,
                        parsed?.Results?.Count);
                    return NullResults(texts.Count);
                }

                return parsed.Results.Select(MapTonality).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось получить тональности от ML-сервиса (батч из {Count} текстов)", texts.Count);
                return NullResults(texts.Count);
            }
        }

        private static double?[] NullResults(int count) => new double?[count];

        private double? MapTonality(PredictResponse? prediction)
        {
            if (prediction?.Label is not int label || prediction.Confidence is not double confidence)
            {
                _logger.LogError("Невалидный ответ ML-сервиса: отсутствуют label/confidence");
                return null;
            }

            switch (label)
            {
                case LabelNegative:
                    return -confidence;
                case LabelPositive:
                    return confidence;
                case LabelNeutral:
                    return 0d;
                default:
                    _logger.LogError("Неизвестный label {Label} в ответе ML-сервиса", label);
                    return null;
            }
        }

        /// <summary>Ответ ML-сервиса: {"text": "...", "label": 0|1|2, "confidence": 0.99}.</summary>
        private sealed class PredictResponse
        {
            [JsonProperty("text")]
            public string? Text { get; set; }

            [JsonProperty("label")]
            public int? Label { get; set; }

            [JsonProperty("confidence")]
            public double? Confidence { get; set; }
        }

        /// <summary>Ответ /predict/batch: {"results": [{...}, ...]} в порядке входных текстов.</summary>
        private sealed class BatchPredictResponse
        {
            [JsonProperty("results")]
            public List<PredictResponse>? Results { get; set; }
        }
    }
}
