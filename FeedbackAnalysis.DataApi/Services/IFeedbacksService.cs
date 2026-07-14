using FeedbackAnalysis.DataApi.Models;

namespace FeedbackAnalysis.DataApi.Services
{
    public interface IFeedbacksService
    {
        Task AddNewFeedbacks(IEnumerable<FeedbackModel> feedbacks);
    }
}
