using FeedbackAnalysis.DataApi.Models;

namespace FeedbackAnalysis.DataApi.Services
{
    public interface IFeedbacksService
    {
        Task<List<FeedbackModel>> GetListAsync(DateTime dateFrom, DateTime dateTo, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)~0);
        Task<List<FeedbackModel>> GetAllAsync();
        Task AddNewFeedbacksAsync(IEnumerable<FeedbackModel> feedbacks);
    }
}
