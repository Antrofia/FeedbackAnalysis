using FeedbackAnalysis.ClientUI.Controllers;
using FeedbackAnalysis.ClientUI.Models;
using FeedbackAnalysis.ClientUI.Services;
using FeedbackAnalysis.Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace FeedbackAnalysis.Tests.ClientUITests;

public class HomeControllerTests
{
    private readonly Mock<IFeedbacksService> _feedbacksServiceMock = new();

    private HomeController CreateSut()
    {
        var sut = new HomeController(_feedbacksServiceMock.Object);

        // TempData инициализируем вручную: контроллер создаётся вне MVC-пайплайна
        sut.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        return sut;
    }

    private void SetupGetFeedbacks(List<FeedbackViewModel>? items = null, int total = 0)
    {
        _feedbacksServiceMock
            .Setup(s => s.GetFeedbacksAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new FeedbacksPage { Items = items ?? [], Total = total });
    }

    [Fact]
    public async Task Index_RequestsFeedbacksWithExpectedWindowAndPageSize()
    {
        DateTime capturedStart = default;
        DateTime capturedEnd = default;
        int capturedPage = 0;
        int capturedPageSize = 0;

        _feedbacksServiceMock
            .Setup(s => s.GetFeedbacksAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<DateTime, DateTime, int, bool, int, int>((start, end, _, _, page, pageSize) =>
            {
                capturedStart = start;
                capturedEnd = end;
                capturedPage = page;
                capturedPageSize = pageSize;
            })
            .ReturnsAsync(new FeedbacksPage());

        var sut = CreateSut();

        await sut.Index();

        Assert.Equal(DateTime.UnixEpoch.AddYears(1), capturedStart);
        Assert.True(capturedEnd <= DateTime.UtcNow);
        Assert.True(capturedEnd > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(1, capturedPage);
        Assert.Equal(20, capturedPageSize);
    }

    [Theory]
    [InlineData("all", (int)FeedbackAnswerStatuses.RequireToAnswer, false)]
    [InlineData("priority", (int)FeedbackAnswerStatuses.RequireToAnswer, true)]
    [InlineData("archive", (int)(FeedbackAnswerStatuses.Answered | FeedbackAnswerStatuses.Archived), false)]
    public async Task Index_PassesStatusAndPriorityAccordingToTab(string tab, int expectedStatus, bool expectedPriority)
    {
        int capturedStatus = 0;
        bool capturedPriority = false;

        _feedbacksServiceMock
            .Setup(s => s.GetFeedbacksAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<DateTime, DateTime, int, bool, int, int>((_, _, status, priority, _, _) =>
            {
                capturedStatus = status;
                capturedPriority = priority;
            })
            .ReturnsAsync(new FeedbacksPage());

        var sut = CreateSut();

        await sut.Index(tab);

        Assert.Equal(expectedStatus, capturedStatus);
        Assert.Equal(expectedPriority, capturedPriority);
    }

    [Fact]
    public async Task Index_UnknownTab_FallsBackToAllFilter()
    {
        int capturedStatus = 0;
        bool capturedPriority = false;

        _feedbacksServiceMock
            .Setup(s => s.GetFeedbacksAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<DateTime, DateTime, int, bool, int, int>((_, _, status, priority, _, _) =>
            {
                capturedStatus = status;
                capturedPriority = priority;
            })
            .ReturnsAsync(new FeedbacksPage());

        var sut = CreateSut();

        var result = await sut.Index("неизвестная-вкладка");

        Assert.Equal((int)FeedbackAnswerStatuses.RequireToAnswer, capturedStatus);
        Assert.False(capturedPriority);

        var model = Assert.IsType<FeedbacksPageViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("all", model.Tab);
    }

    [Fact]
    public async Task Index_PageLessThanOne_DefaultsToOne()
    {
        int capturedPage = 0;

        _feedbacksServiceMock
            .Setup(s => s.GetFeedbacksAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .Callback<DateTime, DateTime, int, bool, int, int>((_, _, _, _, page, _) => capturedPage = page)
            .ReturnsAsync(new FeedbacksPage());

        var sut = CreateSut();

        var result = await sut.Index(page: -3);

        Assert.Equal(1, capturedPage);

        var model = Assert.IsType<FeedbacksPageViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(1, model.Page);
    }

    [Fact]
    public async Task Index_ReturnsPageModelWithItemsAndTotal()
    {
        var items = new List<FeedbackViewModel>
        {
            new() { Id = "wb:11", Sender = "Пётр", Text = "хороший товар", Rating = 5, Tonality = 0.4 },
            new() { Id = "wb:22", Rating = 1 }
        };

        SetupGetFeedbacks(items, total: 42);

        var sut = CreateSut();

        var result = await sut.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<FeedbacksPageViewModel>(viewResult.Model);

        Assert.Equal(2, model.Items.Count);
        Assert.Equal("wb:11", model.Items[0].Id);
        Assert.Equal(42, model.Total);
        Assert.Equal("all", model.Tab);
        Assert.Equal(20, model.PageSize);
    }

    [Fact]
    public async Task Index_EmptyResult_ReturnsViewWithEmptyItems()
    {
        SetupGetFeedbacks(total: 0);

        var sut = CreateSut();

        var result = await sut.Index();

        var model = Assert.IsType<FeedbacksPageViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Empty(model.Items);
        Assert.Equal(0, model.Total);
    }

    [Fact]
    public async Task Reply_ValidText_CallsServiceAndRedirectsToSameTab()
    {
        _feedbacksServiceMock
            .Setup(s => s.SendAnswerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var result = await sut.Reply("wb:11", "Администратор", "Спасибо за отзыв!", "priority");

        _feedbacksServiceMock.Verify(
            s => s.SendAnswerAsync("wb:11", "Администратор", "Спасибо за отзыв!"),
            Times.Once);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("priority", redirect.RouteValues?["tab"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reply_EmptyText_DoesNotCallServiceAndSetsError(string text)
    {
        var sut = CreateSut();

        var result = await sut.Reply("wb:11", "Администратор", text, "all");

        _feedbacksServiceMock.Verify(
            s => s.SendAnswerAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);

        Assert.True(sut.TempData.ContainsKey("ReplyError"));

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("all", redirect.RouteValues?["tab"]);
    }

    [Fact]
    public async Task Archive_CallsServiceAndRedirectsToSameTab()
    {
        _feedbacksServiceMock
            .Setup(s => s.ArchiveAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        var result = await sut.Archive("wb:22", "archive");

        _feedbacksServiceMock.Verify(s => s.ArchiveAsync("wb:22"), Times.Once);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("archive", redirect.RouteValues?["tab"]);
    }
}
