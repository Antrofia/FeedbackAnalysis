using FeedbackAnalysis.DataApi.Repositories.Interfaces;

namespace FeedbackAnalysis.DataApi.UnitOfWork
{
    public interface IUnitOfWork
    {
        IFeedbackRepository FeedbackRepository { get; }
        IFeedbackAnswerStatusRepository FeedbackAnswerStatusRepository { get; }
        IFeedbackTonalityRepository FeedbackTonalityRepository { get; }

        void Save();
    }
}
