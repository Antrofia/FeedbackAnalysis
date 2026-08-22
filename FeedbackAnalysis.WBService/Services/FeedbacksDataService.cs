using FeedbackAnalysis.WBService.Events;
using MediatR;
using System.IO.Compression;
using System.Text;

namespace FeedbackAnalysis.WBService.Services
{
    public class FeedbacksDataService : IFeedbacksDataService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IFeedbacksDataService> _logger;
        private readonly IFeedbackParser _feedbackParser;
        private readonly IMediator _mediator;

        public FeedbacksDataService(
            IConfiguration configuration,
            ILogger<IFeedbacksDataService> logger,
            IFeedbackParser feedbackParser,
            IMediator mediator)
        {
            _configuration = configuration;
            _logger = logger;
            _feedbackParser = feedbackParser;
            _mediator = mediator;
        }

        public async Task SendAllFeedbacksAsync()
        {
            var wbDataSection = _configuration.GetSection("WBApi");
            var host = wbDataSection.GetValue<string>("Host");
            var articles = wbDataSection.GetSection("Articles").Get<string[]>();

            if (articles == null)
            {
                _logger.LogError("{articles} is null", nameof(articles));
                return;
            }

            foreach (var article in articles)
            {
                var json = await FetchArticleJsonAsync(host!, article);
                if (json == null)
                {
                    continue;
                }

                var feedbacks = _feedbackParser.ParseFeedbacks(json);
                if (feedbacks.Count == 0)
                {
                    _logger.LogWarning("No feedbacks parsed for article {article}", article);
                    continue;
                }

                await _mediator.Publish(new FeedbacksGeneratedEvent(feedbacks));
            }
        }

        private async Task<string?> FetchArticleJsonAsync(string host, string article)
        {
            using var httpClient = new HttpClient();

            using var res = await httpClient.GetAsync($"{host}{article}");
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Failed fetch article {article}: {code}", article, res.StatusCode);
                return null;
            }

            if (res.Content.Headers.ContentEncoding.Contains("gzip"))
            {
                await using var responseStream = await res.Content.ReadAsStreamAsync();
                using var decompressedStream = new GZipStream(responseStream, CompressionMode.Decompress);
                using var reader = new StreamReader(decompressedStream, Encoding.UTF8);

                return await reader.ReadToEndAsync();
            }

            return await res.Content.ReadAsStringAsync();
        }
    }
}
