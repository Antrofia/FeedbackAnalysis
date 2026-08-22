using FeedbackAnalysis.WBService.Events;
using MediatR;

namespace FeedbackAnalysis.WBService.Services
{
    public class FeedbacksGeneratedHandler : INotificationHandler<FeedbacksGeneratedEvent>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FeedbacksGeneratedHandler> _logger;
        private readonly HttpClient _httpClient;

        public FeedbacksGeneratedHandler(
            IConfiguration configuration,
            ILogger<FeedbacksGeneratedHandler> logger,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task Handle(FeedbacksGeneratedEvent notification, CancellationToken cancellationToken)
        {
            var dataApiUrl = _configuration.GetSection("Services").GetValue<string>("FeedbacksData");

            var res = await _httpClient.PostAsJsonAsync(
                $"{dataApiUrl?.TrimEnd('/')}/api/feedbacks/add-new",
                new { Feedbacks = notification.Feedbacks },
                cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                var content = await res.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed send feedbacks to DataApi with code: {code}\n{content}", res.StatusCode, content);
            }
        }
    }
}
