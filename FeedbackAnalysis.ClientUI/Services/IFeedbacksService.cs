using FeedbackAnalysis.ClientUI.Models;

namespace FeedbackAnalysis.ClientUI.Services
{
    public interface IFeedbacksService
    {
        Task<List<FeedbackModel>> GetFeedbacksAsync(DateTime startTime, DateTime endTime, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)(-1), int page = 1, int pageSize = 50);
    }
}
