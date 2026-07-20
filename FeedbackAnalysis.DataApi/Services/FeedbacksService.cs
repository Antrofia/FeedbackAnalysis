using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FeedbackAnalysis.DataApi.Services
{
    public class FeedbacksService : IFeedbacksService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FeedbacksService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task AddNewFeedbacksAsync(IEnumerable<FeedbackModel> feedbacks)
        {
            throw new NotImplementedException();
        }

        public async Task<List<FeedbackModel>> GetListAsync(DateTime dateFrom, DateTime dateTo, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)~0)
        {
            var feedbacks = await (await _unitOfWork.FeedbackRepository.FindAsync(x => x.CreatedDate >= dateFrom && x.CreatedDate <= dateTo)).ToListAsync();

            var validStatuses = (await _unitOfWork.FeedbackAnswerStatusRepository.FindAsync(x => status.HasFlag((FeedbackAnswerStatuses)x.StatusId))).ToDictionary(x => x.FeedbackId, x => x);

            var result = new List<FeedbackModel>(5);
            foreach (var feedback in feedbacks)
            {
                if(validStatuses.TryGetValue(feedback.Id, out var statusEntity))
                {
                    result.Add(feedback);
                }
            }
            

            return result;
        }

        public async Task<List<FeedbackModel>> GetAllAsync()
        {
            return (await _unitOfWork.FeedbackRepository.GetAllAsync()).ToList();
        }
    }
}
