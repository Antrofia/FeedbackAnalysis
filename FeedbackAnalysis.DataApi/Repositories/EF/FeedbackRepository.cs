using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Models;
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

        public Task<IQueryable<FeedbackModel>> FindAsync(Expression<Func<FeedbackModel, bool>> predicate)
        {
            return Task.FromResult<IQueryable<FeedbackModel>>(_context.Feedbacks.Where(predicate));
        }

        public Task<IQueryable<FeedbackModel>> GetAllAsync()
        {
            return Task.FromResult<IQueryable<FeedbackModel>>(_context.Feedbacks);
        }

        public async Task<FeedbackModel?> GetAsync(string key)
        {
            return await _context.Feedbacks.FindAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            var target = await GetAsync(key);
            if (target != null)
            {
                _context.Feedbacks.Remove(target);
            }
        }

        public Task UpdateAsync(FeedbackModel entity)
        {
            _context.Feedbacks.Update(entity);
            return Task.CompletedTask;
        }
    }
}
