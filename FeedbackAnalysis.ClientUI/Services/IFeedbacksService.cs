using FeedbackAnalysis.ClientUI.Models;

namespace FeedbackAnalysis.ClientUI.Services
{
    public interface IFeedbacksService
    {
        Task<FeedbacksPage> GetFeedbacksAsync(DateTime dateFrom, DateTime dateTo, int status, bool priority, int page = 1, int pageSize = 20);

        Task<bool> SendAnswerAsync(string feedbackId, string sender, string text);

        Task<bool> ArchiveAsync(string feedbackId);
    }
}
