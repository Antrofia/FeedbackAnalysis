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
        private readonly IFeedbackAnswerRepository _feedbackAnswerRepository;

        public IFeedbackRepository FeedbackRepository => _feedbackRepository;
        public IFeedbackAnswerStatusRepository FeedbackAnswerStatusRepository => _feedbackAnswerStatusRepository;
        public IFeedbackTonalityRepository FeedbackTonalityRepository => _feedbackTonalityRepository;
        public IFeedbackAnswerRepository FeedbackAnswerRepository => _feedbackAnswerRepository;

        public EFUnitOfWork(
            EFContext context,
            IFeedbackRepository feedbackRepository,
            IFeedbackAnswerStatusRepository feedbackAnswerStatusRepository,
            IFeedbackTonalityRepository feedbackTonalityRepository,
            IFeedbackAnswerRepository feedbackAnswerRepository)
        {
            _context = context;
            _feedbackRepository = feedbackRepository;
            _feedbackAnswerStatusRepository = feedbackAnswerStatusRepository;
            _feedbackTonalityRepository = feedbackTonalityRepository;
            _feedbackAnswerRepository = feedbackAnswerRepository;
        }

        public Task SaveAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
