using FeedbackAnalysis.Contracts.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace FeedbackAnalysis.FakeFeedbacksService.Services
{
    public class FeedbacksPublisher : IFeedbacksPublisher
    {
        // DataApi ждёт тело вида {"feedbacks": [...]} с полями в camelCase
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IFeedbacksPublisher> _logger;

        public FeedbacksPublisher(HttpClient httpClient, IConfiguration configuration, ILogger<IFeedbacksPublisher> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task PublishAsync(List<FeedbackModel> feedbacks, CancellationToken cancellationToken = default)
        {
            var dataApiHost = _configuration.GetSection("Services").GetValue<string>("FeedbacksData");

            if (string.IsNullOrWhiteSpace(dataApiHost))
            {
                _logger.LogError(
                    "Не задан адрес DataApi (Services:FeedbacksData) — батч из {count} отзывов не отправлен",
                    feedbacks.Count);
                return;
            }

            var url = $"{dataApiHost.TrimEnd('/')}/api/feedbacks/add-new";

            try
            {
                var payload = JsonConvert.SerializeObject(new { feedbacks }, JsonSettings);

                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "DataApi вернул ошибку при публикации батча из {count} фейковых отзывов ({code}) {url}: {body}",
                        feedbacks.Count,
                        (int)response.StatusCode,
                        url,
                        await response.Content.ReadAsStringAsync(cancellationToken));
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
            {
                _logger.LogError(
                    ex,
                    "Не удалось отправить батч из {count} фейковых отзывов в DataApi ({url})",
                    feedbacks.Count,
                    url);
            }
        }
    }
}
