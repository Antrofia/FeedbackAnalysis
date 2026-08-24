namespace FeedbackAnalysis.ClientUI.Models
{
    /// <summary>Модель представления: список отзывов + текущая вкладка и пагинация.</summary>
    public class FeedbacksPageViewModel
    {
        public IReadOnlyList<FeedbackViewModel> Items { get; init; } = [];
        public string Tab { get; init; } = "all";
        public int Page { get; init; } = 1;
        public int PageSize { get; init; }
        public int Total { get; init; }

        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 0;
        public bool HasPrev => Page > 1;
        public bool HasNext => Page < TotalPages;

        // Счётчики всех вкладок — показываются на переключателе одновременно
        public int CountAll { get; init; }
        public int CountPriority { get; init; }
        public int CountArchive { get; init; }
    }
}
