using System.IO.Compression;
using System.Text;

namespace FeedbackAnalysis.WBService.Services
{
    public class FeedbacksDataService : IFeedbacksDataService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IFeedbacksDataService> _logger;
        private readonly IFeedbackParser _feedbackParser;

        public FeedbacksDataService(
            IConfiguration configuration, 
            ILogger<IFeedbacksDataService> logger,
            IFeedbackParser feedbackParser)
        {
            _configuration = configuration;
            _logger = logger;
            _feedbackParser = feedbackParser;
        }

        public async Task SendAllFeedbacksAsync()
        {
            var _httpClient = new HttpClient();

            var wbDataSection = _configuration.GetSection("WBApi");
            var host = wbDataSection.GetValue<string>("Host");
            var articles = wbDataSection.GetSection("Articles").Get<string[]>();

            if(articles == null)
            {
                _logger.LogError("{articles} is null", nameof(articles));
                return;
            }


            foreach (var article in articles)
            {
                var res = await _httpClient.GetAsync($"{host}{article}");

                using var responseStream = await res.Content.ReadAsStreamAsync();
                using var decompressedStream = new GZipStream(responseStream, CompressionMode.Decompress);
                using var reader = new StreamReader(decompressedStream, Encoding.UTF8);

                string json = reader.ReadToEnd();


                var feedbacks = _feedbackParser.ParseFeedbacks(json);

                var res1 = await _httpClient.PostAsJsonAsync(_configuration.GetSection("Services").GetValue<string>("FeedbacksData"), new
                {
                    Feedbacks = feedbacks
                });

                if (!res1.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed send feedback with code: {code}", res1.StatusCode);
                }
            }
        }
    }
}
