using FeedbackAnalysis.Contracts.Models;

namespace FeedbackAnalysis.DataApi.Services
{
    public interface IFeedbacksService
    {
        Task<PagedResult<FeedbackDetailedModel>> GetListAsync(DateTime dateFrom, DateTime dateTo, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)~0, int page = 1, int pageSize = 50, bool priorityOnly = false);
        Task<List<FeedbackModel>> GetAllAsync();
        Task AddNewFeedbacksAsync(IEnumerable<FeedbackModel> feedbacks);
        Task<bool> AnswerFeedbackAsync(FeedbackAnswerRequest request);
        Task<bool> ArchiveFeedbackAsync(string feedbackId);
    }
}
