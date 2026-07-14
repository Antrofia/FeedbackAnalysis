using FeedbackAnalysis.DataApi.Models;

namespace FeedbackAnalysis.DataApi.Services
{
    public class FeedbacksService : IFeedbacksService
    {
        public FeedbacksService()
        {
            
        }

        public Task AddNewFeedbacks(IEnumerable<FeedbackModel> feedbacks)
        {
            throw new NotImplementedException();
        }
    }
}
