namespace FeedbackAnalysis.ReviewsHandlerApi.Services
{
    public interface ISentimentService
    {
        Task<int?> AnalyzeSentimentAsync(string text);
    }
}
