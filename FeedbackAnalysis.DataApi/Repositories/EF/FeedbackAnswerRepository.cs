using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.Repositories.Interfaces;
using System.Linq.Expressions;

namespace FeedbackAnalysis.DataApi.Repositories.EF
{
    public class FeedbackAnswerRepository : IFeedbackAnswerRepository
    {
        private readonly EFContext _context;

        public FeedbackAnswerRepository(EFContext context)
        {
            _context = context;
        }

        public async Task AddAsync(FeedbackAnswerModel entity)
        {
            await _context.FeedbacksAnswers.AddAsync(entity);
        }

        public IQueryable<FeedbackAnswerModel> Find(Expression<Func<FeedbackAnswerModel, bool>> predicate)
        {
            return _context.FeedbacksAnswers.Where(predicate);
        }

        public IQueryable<FeedbackAnswerModel> GetAll()
        {
            return _context.FeedbacksAnswers;
        }

        public ValueTask<FeedbackAnswerModel?> GetAsync(string key)
        {
            return _context.FeedbacksAnswers.FindAsync(key);
        }

        public async Task RemoveAsync(string key)
        {
            var target = await GetAsync(key);
            if (target != null)
            {
                _context.FeedbacksAnswers.Remove(target);
            }
        }

        public void Update(FeedbackAnswerModel entity)
        {
            _context.FeedbacksAnswers.Update(entity);
        }
    }
}
