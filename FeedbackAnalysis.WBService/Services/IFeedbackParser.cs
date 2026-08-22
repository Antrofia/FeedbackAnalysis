using FeedbackAnalysis.Contracts.Models;

namespace FeedbackAnalysis.WBService.Services
{
    public interface IFeedbackParser
    {
        List<FeedbackModel> ParseFeedbacks(string jsonResponse);
    }
}
