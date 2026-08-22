using FeedbackAnalysis.ClientUI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.Json;

namespace FeedbackAnalysis.ClientUI.Services
{
    public class FeedbacksService : IFeedbacksService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IFeedbacksService> _logger;

        public FeedbacksService(
            IConfiguration configuration, 
            ILogger<IFeedbacksService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<FeedbackModel>> GetFeedbacksAsync(DateTime startTime, DateTime endTime, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)(-1))
        {

            var _httpClient = new HttpClient();

            var services = _configuration.GetSection("Services");
            var feedbacksDataHost = services.GetValue<string>("FeedbacksData");


            var dFrom = new DateTimeOffset(startTime).ToUnixTimeSeconds();
            var dTo = new DateTimeOffset(endTime).ToUnixTimeSeconds();

            var res = await _httpClient.GetAsync($"{feedbacksDataHost}api/feedbacks/list?dateFrom={dFrom}&dateTo={dTo}");

            if(res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<JObject>(json);

                return data["feedbacks"].ToObject<List<FeedbackModel>>();
            }
            else
            {
                _logger.LogError("Failed request to feedbacks data (Code {code})\n{content}", res.StatusCode, await res.Content.ReadAsStringAsync());
                return [];
            }
        }
    }
}
