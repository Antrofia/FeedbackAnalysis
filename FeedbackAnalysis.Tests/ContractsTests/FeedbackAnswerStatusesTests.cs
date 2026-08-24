using FeedbackAnalysis.Contracts.Models;

namespace FeedbackAnalysis.Tests.ContractsTests;

public class FeedbackAnswerStatusesTests
{
    [Theory]
    [InlineData(FeedbackAnswerStatuses.None, 0)]
    [InlineData(FeedbackAnswerStatuses.RequireToAnswer, 1)]
    [InlineData(FeedbackAnswerStatuses.Answered, 2)]
    [InlineData(FeedbackAnswerStatuses.Archived, 4)]
    [InlineData(FeedbackAnswerStatuses.NotHandled, 8)]
    public void Flags_HaveStablePowerOfTwoValues(FeedbackAnswerStatuses value, int expected)
    {
        Assert.Equal(expected, (int)value);
    }

    [Fact]
    public void CombiningFlags_PreservesAllBits()
    {
        var combined = FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.Answered;

        Assert.Equal(3, (int)combined);
        Assert.True(combined.HasFlag(FeedbackAnswerStatuses.RequireToAnswer));
        Assert.True(combined.HasFlag(FeedbackAnswerStatuses.Answered));
        Assert.False(combined.HasFlag(FeedbackAnswerStatuses.Archived));
    }

    [Fact]
    public void AllDefinedFlags_AreDistinct()
    {
        var all = new[]
        {
            FeedbackAnswerStatuses.RequireToAnswer,
            FeedbackAnswerStatuses.Answered,
            FeedbackAnswerStatuses.Archived,
            FeedbackAnswerStatuses.NotHandled
        };

        Assert.Equal(all.Length, all.Select(x => (int)x).Distinct().Count());
    }

    [Theory]
    [InlineData((int)FeedbackAnswerStatuses.RequireToAnswer, (int)FeedbackAnswerStatuses.RequireToAnswer, true)]
    [InlineData((int)FeedbackAnswerStatuses.RequireToAnswer | 2, (int)FeedbackAnswerStatuses.Answered, true)]
    [InlineData((int)FeedbackAnswerStatuses.NotHandled, (int)FeedbackAnswerStatuses.Archived, false)]
    [InlineData(0, (int)FeedbackAnswerStatuses.None, false)]
    public void FlagIntersection_BehavesLikeBitwiseAnd(int statusId, int mask, bool expected)
    {
        var actual = (statusId & mask) != 0;

        Assert.Equal(expected, actual);
    }
}
