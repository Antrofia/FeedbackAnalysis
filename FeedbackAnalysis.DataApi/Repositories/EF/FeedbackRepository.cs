using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;
using System.Linq.Expressions;

namespace FeedbackAnalysis.DataApi.Repositories.EF
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly EFContext _context;

        public FeedbackRepository(EFContext context)
        {
            _context = context;
        }

        public async Task AddAsync(FeedbackModel entity)
        {
            await _context.Feedbacks.AddAsync(entity);
        }

        public IQueryable<FeedbackModel> Find(Expression<Func<FeedbackModel, bool>> predicate)
        {
            return _context.Feedbacks.Where(predicate);
        }

        public IQueryable<FeedbackModel> GetAll()
        {
            return _context.Feedbacks;
        }

        public ValueTask<FeedbackModel?> GetAsync(string key)
        {
            return _context.Feedbacks.FindAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            var target = await GetAsync(key);
            if (target != null)
            {
                _context.Feedbacks.Remove(target);
            }
        }

        public void Update(FeedbackModel entity)
        {
            _context.Feedbacks.Update(entity);
        }
    }
}
