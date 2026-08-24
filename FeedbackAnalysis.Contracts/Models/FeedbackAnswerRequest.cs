namespace FeedbackAnalysis.Contracts.Models
{
    /// <summary>Запрос на сохранение ответа оператора на отзыв.</summary>
    public class FeedbackAnswerRequest
    {
        public string FeedbackId { get; set; } = "";
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
