namespace FeedbackAnalysis.ClientUI.Models
{
    /// <summary>Страница отзывов, полученная из DataApi.</summary>
    public class FeedbacksPage
    {
        public IReadOnlyList<FeedbackViewModel> Items { get; init; } = [];
        public int Total { get; init; }
    }
}
