using System.Net.Http.Headers;
using System.Text;
using AutoMapper;
using FeedbackAnalysis.ClientUI.Models;
using FeedbackAnalysis.Contracts.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FeedbackAnalysis.ClientUI.Services
{
    public class FeedbacksService : IFeedbacksService
    {
        private sealed class FeedbacksListResponse
        {
            public List<FeedbackDetailedModel> Feedbacks { get; set; } = [];
            public int Total { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }

        private static readonly JsonSerializerSettings CamelCaseSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ILogger<IFeedbacksService> _logger;

        public FeedbacksService(
            HttpClient httpClient,
            IConfiguration configuration,
            IMapper mapper,
            ILogger<IFeedbacksService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<FeedbacksPage> GetFeedbacksAsync(DateTime dateFrom, DateTime dateTo, int status, bool priority, int page = 1, int pageSize = 20)
        {
            var feedbacksDataHost = GetDataApiHost();

            var dFrom = new DateTimeOffset(dateFrom).ToUnixTimeSeconds();
            var dTo = new DateTimeOffset(dateTo).ToUnixTimeSeconds();

            var url = $"{feedbacksDataHost}/api/feedbacks/list?dateFrom={dFrom}&dateTo={dTo}&status={status}&priority={(priority ? "true" : "false")}&page={page}&pageSize={pageSize}";

            var res = await _httpClient.GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Не удалось получить список отзывов (код {code})\n{content}", res.StatusCode, await res.Content.ReadAsStringAsync());
                return new FeedbacksPage();
            }

            var data = JsonConvert.DeserializeObject<FeedbacksListResponse>(await res.Content.ReadAsStringAsync());

            return new FeedbacksPage
            {
                Items = data is null ? [] : _mapper.Map<List<FeedbackViewModel>>(data.Feedbacks),
                Total = data?.Total ?? 0
            };
        }

        public async Task<bool> SendAnswerAsync(string feedbackId, string sender, string text)
        {
            var url = $"{GetDataApiHost()}/api/feedbacks/answer";

            var body = JsonConvert.SerializeObject(new FeedbackAnswerRequest
            {
                FeedbackId = feedbackId,
                Sender = sender,
                Text = text
            }, CamelCaseSettings);

            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var res = await _httpClient.PostAsync(url, content);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Не удалось отправить ответ на отзыв {feedbackId} (код {code})\n{content}", feedbackId, res.StatusCode, await res.Content.ReadAsStringAsync());
                return false;
            }

            return true;
        }

        public async Task<bool> ArchiveAsync(string feedbackId)
        {
            var url = $"{GetDataApiHost()}/api/feedbacks/archive/{Uri.EscapeDataString(feedbackId)}";

            var res = await _httpClient.PostAsync(url, content: null);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Не удалось архивировать отзыв {feedbackId} (код {code})\n{content}", feedbackId, res.StatusCode, await res.Content.ReadAsStringAsync());
                return false;
            }

            return true;
        }

        private string GetDataApiHost()
        {
            var host = _configuration.GetSection("Services").GetValue<string>("FeedbacksData");

            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("Не задан адрес DataApi: конфигурация Services:FeedbacksData.");
            }

            return host.TrimEnd('/');
        }
    }
}
