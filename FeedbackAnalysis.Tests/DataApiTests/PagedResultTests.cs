using FeedbackAnalysis.DataApi.Services;

namespace FeedbackAnalysis.Tests.DataApiTests;

public class PagedResultTests
{
    [Fact]
    public void DefaultItems_AreEmptyAndNotNull()
    {
        var result = new PagedResult<string>();

        Assert.NotNull(result.Items);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.Page);
        Assert.Equal(0, result.PageSize);
    }

    [Fact]
    public void InitProperties_AreStored()
    {
        var items = new[] { "a", "b" };
        var result = new PagedResult<string>
        {
            Items = items,
            Total = 10,
            Page = 2,
            PageSize = 2
        };

        Assert.Equal(items, result.Items);
        Assert.Equal(10, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
    }
}
