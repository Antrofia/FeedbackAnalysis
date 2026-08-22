using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;
using System.Linq.Expressions;

namespace FeedbackAnalysis.DataApi.Repositories.EF
{
    public class FeedbackAnswerStatusRepository : IFeedbackAnswerStatusRepository
    {
        private readonly EFContext _context;

        public FeedbackAnswerStatusRepository(EFContext context)
        {
            _context = context;
        }

        public async Task AddAsync(FeedbackAnswerStatusModel entity)
        {
            await _context.FeedbacksAnswerStatus.AddAsync(entity);
        }

        public IQueryable<FeedbackAnswerStatusModel> Find(Expression<Func<FeedbackAnswerStatusModel, bool>> predicate)
        {
            return _context.FeedbacksAnswerStatus.Where(predicate);
        }

        public IQueryable<FeedbackAnswerStatusModel> GetAll()
        {
            return _context.FeedbacksAnswerStatus;
        }

        public ValueTask<FeedbackAnswerStatusModel?> GetAsync(string key)
        {
            return _context.FeedbacksAnswerStatus.FindAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            var target = await GetAsync(key);
            if (target != null)
            {
                _context.FeedbacksAnswerStatus.Remove(target);
            }
        }

        public void Update(FeedbackAnswerStatusModel entity)
        {
            _context.FeedbacksAnswerStatus.Update(entity);
        }
    }
}
