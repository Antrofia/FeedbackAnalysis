using FeedbackAnalysis.WBService.Models;

namespace FeedbackAnalysis.WBService.Services
{
    public interface IFeedbackParser
    {
        List<FeedbackModel> ParseFeedbacks(string jsonResponse);
    }
}
