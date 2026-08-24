namespace FeedbackAnalysis.Contracts.Models
{
    /// <summary>
    /// Отзыв с обогащением для API-ответов (DTO, в БД не маппится).
    /// </summary>
    public class FeedbackDetailedModel : FeedbackModel
    {
        public double? Tonality { get; set; }
        public FeedbackAnswerStatuses? Status { get; set; }
        public string? AnswerSender { get; set; }
        public string? AnswerText { get; set; }
    }
}
