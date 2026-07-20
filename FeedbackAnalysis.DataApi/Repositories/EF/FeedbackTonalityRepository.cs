using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;
using System.Linq.Expressions;

namespace FeedbackAnalysis.DataApi.Repositories.EF
{
    public class FeedbackTonalityRepository : IFeedbackTonalityRepository
    {
        private readonly EFContext _context;

        public FeedbackTonalityRepository(EFContext context)
        {
            _context = context;
        }
        public async Task AddAsync(FeedbackTonalityModel entity)
        {
            await _context.FeedbacksTonality.AddAsync(entity);
        }

        public Task<IQueryable<FeedbackTonalityModel>> FindAsync(Expression<Func<FeedbackTonalityModel, bool>> predicate)
        {
            return Task.FromResult<IQueryable<FeedbackTonalityModel>>(_context.FeedbacksTonality.Where(predicate));
        }

        public Task<IQueryable<FeedbackTonalityModel>> GetAllAsync()
        {
            return Task.FromResult<IQueryable<FeedbackTonalityModel>>(_context.FeedbacksTonality);
        }

        public async Task<FeedbackTonalityModel?> GetAsync(string key)
        {
            return await _context.FeedbacksTonality.FindAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            var target = await GetAsync(key);
            if (target != null)
            {
                _context.FeedbacksTonality.Remove(target);
            }
        }

        public Task UpdateAsync(FeedbackTonalityModel entity)
        {
            _context.FeedbacksTonality.Update(entity);
            return Task.CompletedTask;
        }
    }
}
