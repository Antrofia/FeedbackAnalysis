using FeedbackAnalysis.Contracts.Models;

namespace FeedbackAnalysis.FakeFeedbacksService.Services
{
    public interface IFakeFeedbacksGenerator
    {
        /// <summary>Генерирует батч фейковых отзывов заданного размера.</summary>
        List<FeedbackModel> GenerateBatch(int count);
    }
}
