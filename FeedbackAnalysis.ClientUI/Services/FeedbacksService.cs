using FeedbackAnalysis.ClientUI.Models;
using Newtonsoft.Json;

namespace FeedbackAnalysis.ClientUI.Services
{
    public class FeedbacksService : IFeedbacksService
    {
        private sealed class FeedbacksListResponse
        {
            public List<FeedbackModel> Feedbacks { get; set; } = [];
            public int Total { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
        }

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IFeedbacksService> _logger;

        public FeedbacksService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<IFeedbacksService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<FeedbackModel>> GetFeedbacksAsync(DateTime startTime, DateTime endTime, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)(-1), int page = 1, int pageSize = 50)
        {
            var feedbacksDataHost = _configuration.GetSection("Services").GetValue<string>("FeedbacksData");

            var dFrom = new DateTimeOffset(startTime).ToUnixTimeSeconds();
            var dTo = new DateTimeOffset(endTime).ToUnixTimeSeconds();

            var url = $"{feedbacksDataHost?.TrimEnd('/')}/api/feedbacks/list?dateFrom={dFrom}&dateTo={dTo}&page={page}&pageSize={pageSize}";

            if ((int)status != -1)
            {
                url += $"&status={(int)status}";
            }

            var res = await _httpClient.GetAsync(url);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Failed request to feedbacks data (Code {code})\n{content}", res.StatusCode, await res.Content.ReadAsStringAsync());
                return [];
            }

            var data = JsonConvert.DeserializeObject<FeedbacksListResponse>(await res.Content.ReadAsStringAsync());

            return data?.Feedbacks ?? [];
        }
    }
}
