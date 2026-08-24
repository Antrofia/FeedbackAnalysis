using FeedbackAnalysis.Contracts.Models;

namespace FeedbackAnalysis.FakeFeedbacksService.Services
{
    public interface IFeedbacksPublisher
    {
        /// <summary>Отправляет батчик отзывов в DataApi; ошибки наружу не бросает.</summary>
        Task PublishAsync(List<FeedbackModel> feedbacks, CancellationToken cancellationToken = default);
    }
}
