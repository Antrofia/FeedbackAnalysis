using System.Linq.Expressions;

namespace FeedbackAnalysis.DataApi.Repositories.Interfaces
{
    public interface IRepository<T, TKey>
    {
        IQueryable<T> GetAll();
        ValueTask<T?> GetAsync(TKey key);
        IQueryable<T> Find(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);
        void Update(T entity);
        Task RemoveAsync(TKey key);
    }
}
