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

        public Task AddAsync(FeedbackModel entity)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<FeedbackModel>> FindAllAsync(Expression<FeedbackModel> predicate)
        {
            throw new NotImplementedException();
        }

        public Task<FeedbackModel> FindAsync(Expression<FeedbackModel> predicate)
        {
            throw new NotImplementedException();
        }

        public Task<FeedbackModel> GetAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key)
        {
            throw new NotImplementedException();
        }

        public Task UpdateAsync(FeedbackModel entity)
        {
            throw new NotImplementedException();
        }
    }
}
