using FeedbackAnalysis.DataApi.Repositories.Interfaces;

namespace FeedbackAnalysis.DataApi.UnitOfWork
{
    public interface IUnitOfWork
    {
        IFeedbackRepository FeedbackRepository { get; }
    }
}
