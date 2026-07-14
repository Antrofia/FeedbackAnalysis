using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Repositories.EF;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;

namespace FeedbackAnalysis.DataApi.UnitOfWork
{
    public class EFUnitOfWork : IUnitOfWork
    {
        private readonly EFContext _context;


        private IFeedbackRepository? _feedbackRepository;

        public IFeedbackRepository FeedbackRepository => _feedbackRepository ??= new FeedbackRepository(_context);


        public EFUnitOfWork(EFContext context)
        {
            this._context = context;
        }
    }
}
