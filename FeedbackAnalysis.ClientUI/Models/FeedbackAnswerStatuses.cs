namespace FeedbackAnalysis.ClientUI.Models
{
    [Flags]
    public enum FeedbackAnswerStatuses
    {
        None = 0,
        RequireToAnswer = 1,
        Answered = 2,
        Archived = 4,
        NotHandled = 8
    }
}
