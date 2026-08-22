using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Repositories.EF;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;

namespace FeedbackAnalysis.DataApi.UnitOfWork
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly EFContext _context;

        private IFeedbackRepository? _feedbackRepository;
        private IFeedbackAnswerStatusRepository? _feedbackAnswerStatusRepository;
        private IFeedbackTonalityRepository? _feedbackTonalityRepository;

        public IFeedbackRepository FeedbackRepository => _feedbackRepository ??= new FeedbackRepository(_context);
        public IFeedbackAnswerStatusRepository FeedbackAnswerStatusRepository => _feedbackAnswerStatusRepository ??= new FeedbackAnswerStatusRepository(_context);
        public IFeedbackTonalityRepository FeedbackTonalityRepository => _feedbackTonalityRepository ??= new FeedbackTonalityRepository(_context);

        public EFUnitOfWork(EFContext context)
        {
            _context = context;
        }

        public Task SaveAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
