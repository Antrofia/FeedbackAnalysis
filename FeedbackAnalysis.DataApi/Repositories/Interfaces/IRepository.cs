using System.Linq.Expressions;

namespace FeedbackAnalysis.DataApi.Repositories.Interfaces
{
    public interface IRepository<T, TKey>
    {
        Task<IQueryable<T>> GetAllAsync();
        Task<T?> GetAsync(TKey key);
        Task<IQueryable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task RemoveAsync(TKey key);
    }
}
