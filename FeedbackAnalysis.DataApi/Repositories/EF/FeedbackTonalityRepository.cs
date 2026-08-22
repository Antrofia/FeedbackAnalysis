using FeedbackAnalysis.Contracts.Models;
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

        public IQueryable<FeedbackTonalityModel> Find(Expression<Func<FeedbackTonalityModel, bool>> predicate)
        {
            return _context.FeedbacksTonality.Where(predicate);
        }

        public IQueryable<FeedbackTonalityModel> GetAll()
        {
            return _context.FeedbacksTonality;
        }

        public ValueTask<FeedbackTonalityModel?> GetAsync(string key)
        {
            return _context.FeedbacksTonality.FindAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            var target = await GetAsync(key);
            if (target != null)
            {
                _context.FeedbacksTonality.Remove(target);
            }
        }

        public void Update(FeedbackTonalityModel entity)
        {
            _context.FeedbacksTonality.Update(entity);
        }
    }
}
