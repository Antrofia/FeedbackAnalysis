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

        public Task<IQueryable<FeedbackAnswerStatusModel>> FindAsync(Expression<Func<FeedbackAnswerStatusModel, bool>> predicate)
        {
            return Task.FromResult<IQueryable<FeedbackAnswerStatusModel>>(_context.FeedbacksAnswerStatus.Where(predicate));
        }

        public Task<IQueryable<FeedbackAnswerStatusModel>> GetAllAsync()
        {
            return Task.FromResult<IQueryable<FeedbackAnswerStatusModel>>(_context.FeedbacksAnswerStatus);
        }

        public async Task<FeedbackAnswerStatusModel?> GetAsync(string key)
        {
            return await _context.FeedbacksAnswerStatus.FindAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            var target = await GetAsync(key);
            if (target != null)
            {
                _context.FeedbacksAnswerStatus.Remove(target);
            }
        }

        public Task UpdateAsync(FeedbackAnswerStatusModel entity)
        {
            _context.FeedbacksAnswerStatus.Update(entity);
            return Task.CompletedTask;
        }
    }
}
