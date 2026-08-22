using FeedbackAnalysis.Contracts.Models;

namespace FeedbackAnalysis.DataApi.Services
{
    public interface IFeedbacksService
    {
        Task<PagedResult<FeedbackModel>> GetListAsync(DateTime dateFrom, DateTime dateTo, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)~0, int page = 1, int pageSize = 50);
        Task<List<FeedbackModel>> GetAllAsync();
        Task AddNewFeedbacksAsync(IEnumerable<FeedbackModel> feedbacks);
    }
}
