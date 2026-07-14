namespace FeedbackAnalysis.DataApi.Models
{
    public class AddNewFeedbacksModel
    {
        public List<FeedbackModel> Feedbacks { get; set; } = new List<FeedbackModel>();
    }
}
