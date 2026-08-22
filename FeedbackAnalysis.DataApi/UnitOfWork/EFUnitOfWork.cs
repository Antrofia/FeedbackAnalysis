using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;

namespace FeedbackAnalysis.DataApi.UnitOfWork
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly EFContext _context;
        private readonly IFeedbackRepository _feedbackRepository;
        private readonly IFeedbackAnswerStatusRepository _feedbackAnswerStatusRepository;
        private readonly IFeedbackTonalityRepository _feedbackTonalityRepository;

        public IFeedbackRepository FeedbackRepository => _feedbackRepository;
        public IFeedbackAnswerStatusRepository FeedbackAnswerStatusRepository => _feedbackAnswerStatusRepository;
        public IFeedbackTonalityRepository FeedbackTonalityRepository => _feedbackTonalityRepository;

        public EFUnitOfWork(
            EFContext context,
            IFeedbackRepository feedbackRepository,
            IFeedbackAnswerStatusRepository feedbackAnswerStatusRepository,
            IFeedbackTonalityRepository feedbackTonalityRepository)
        {
            _context = context;
            _feedbackRepository = feedbackRepository;
            _feedbackAnswerStatusRepository = feedbackAnswerStatusRepository;
            _feedbackTonalityRepository = feedbackTonalityRepository;
        }

        public Task SaveAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
