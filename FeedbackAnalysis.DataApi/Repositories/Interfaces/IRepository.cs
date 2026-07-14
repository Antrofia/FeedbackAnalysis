using System.Linq.Expressions;

namespace FeedbackAnalysis.DataApi.Repositories.Interfaces
{
    public interface IRepository<T, TKey>
    {
        Task<T> GetAsync(TKey key);
        Task<T> FindAsync(Expression<T> predicate);
        Task<IEnumerable<T>> FindAllAsync(Expression<T> predicate);

        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task RemoveAsync(TKey key);
    }
}
