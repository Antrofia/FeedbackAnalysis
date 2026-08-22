using FeedbackAnalysis.WBService.Models;

namespace FeedbackAnalysis.WBService.Events
{
    public class FeedbacksGeneratedEvent : INotification
    {
        public List<FeedbackModel> Feedbacks { get; }

        public FeedbacksGeneratedEvent(List<FeedbackModel> feedbacks)
        {
            Feedbacks = feedbacks;
        }
    }
}
