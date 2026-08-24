namespace FeedbackAnalysis.ClientUI.Models
{
    public class FeedbackViewModel
    {
        public string Id { get; set; } = "";

        public double Rating { get; set; } = 0;
        public string? Sender { get; set; }
        public string? Text { get; set; }
        public DateTime? CreatedDate { get; set; }

        public string? NomenclatureLink { get; set; }

        /// <summary>Тональность отзыва, −1..+1 (null, если ещё не рассчитана).</summary>
        public double? Tonality { get; set; }

        /// <summary>Биты флагов FeedbackAnswerStatuses.</summary>
        public int? Status { get; set; }
        public string? AnswerSender { get; set; }
        public string? AnswerText { get; set; }
    }
}
