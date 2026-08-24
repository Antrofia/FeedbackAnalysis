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
    private const int MainPageSize = 20;

    private readonly Mock<IFeedbacksService> _feedbacksServiceMock = new();

    // Все вызовы GetFeedbacksAsync за тест: основной запрос страницы + запросы-счётчики вкладок
    private readonly List<(DateTime Start, DateTime End, int Status, bool Priority, int Page, int PageSize)> _invocations = new();

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
            .Callback<DateTime, DateTime, int, bool, int, int>((start, end, status, priority, page, pageSize) =>
                _invocations.Add((start, end, status, priority, page, pageSize)))
            .ReturnsAsync(new FeedbacksPage { Items = items ?? [], Total = total });
    }

    // Основной запрос страницы — единственный вызов с полным размером страницы
    private (DateTime Start, DateTime End, int Status, bool Priority, int Page, int PageSize) MainCall =>
        _invocations.Single(c => c.PageSize == MainPageSize);

    [Fact]
    public async Task Index_RequestsFeedbacksWithExpectedWindowAndPageSize()
    {
        SetupGetFeedbacks();

        var sut = CreateSut();

        await sut.Index();

        Assert.Equal(DateTime.UnixEpoch.AddYears(1), MainCall.Start);
        Assert.True(MainCall.End <= DateTime.UtcNow);
        Assert.True(MainCall.End > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(1, MainCall.Page);
        Assert.Equal(20, MainCall.PageSize);
    }

    [Theory]
    [InlineData("all", (int)FeedbackAnswerStatuses.RequireToAnswer, false)]
    [InlineData("priority", (int)FeedbackAnswerStatuses.RequireToAnswer, true)]
    [InlineData("archive", (int)(FeedbackAnswerStatuses.Answered | FeedbackAnswerStatuses.Archived), false)]
    public async Task Index_PassesStatusAndPriorityAccordingToTab(string tab, int expectedStatus, bool expectedPriority)
    {
        SetupGetFeedbacks();

        var sut = CreateSut();

        await sut.Index(tab);

        Assert.Equal(expectedStatus, MainCall.Status);
        Assert.Equal(expectedPriority, MainCall.Priority);
    }

    [Fact]
    public async Task Index_UnknownTab_FallsBackToAllFilter()
    {
        SetupGetFeedbacks();

        var sut = CreateSut();

        var result = await sut.Index("неизвестная-вкладка");

        Assert.Equal((int)FeedbackAnswerStatuses.RequireToAnswer, MainCall.Status);
        Assert.False(MainCall.Priority);

        var model = Assert.IsType<FeedbacksPageViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("all", model.Tab);
    }

    [Fact]
    public async Task Index_PageLessThanOne_DefaultsToOne()
    {
        SetupGetFeedbacks();

        var sut = CreateSut();

        var result = await sut.Index(page: -3);

        Assert.Equal(1, MainCall.Page);

        var model = Assert.IsType<FeedbacksPageViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(1, model.Page);
    }

    [Fact]
    public async Task Index_RequestsCountersForEveryOtherTabWithMinimalPage()
    {
        SetupGetFeedbacks(total: 7);

        var sut = CreateSut();

        await sut.Index("all");

        // Основной запрос + счётчики двух остальных вкладок
        Assert.Equal(3, _invocations.Count);

        var counterCalls = _invocations.Where(c => c.PageSize != MainPageSize).ToList();
        Assert.Equal(2, counterCalls.Count);

        // Счётчикам достаточно Total: минимальная страница
        Assert.All(counterCalls, c =>
        {
            Assert.Equal(1, c.Page);
            Assert.Equal(1, c.PageSize);
        });

        // Затронуты обе оставшиеся вкладки: приоритетные и архив
        var filters = counterCalls.Select(c => (c.Status, c.Priority)).ToHashSet();
        Assert.Contains(((int)FeedbackAnswerStatuses.RequireToAnswer, true), filters);
        Assert.Contains(((int)(FeedbackAnswerStatuses.Answered | FeedbackAnswerStatuses.Archived), false), filters);
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
    public async Task Index_ReturnsCountsForAllTabs()
    {
        // Total зависит от запрошенного фильтра: эмулируем разные счётчики вкладок
        _feedbacksServiceMock
            .Setup(s => s.GetFeedbacksAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((DateTime _, DateTime _, int status, bool priority, int _, int _) =>
            {
                var total = (status, priority) switch
                {
                    ((int)FeedbackAnswerStatuses.RequireToAnswer, true) => 3,
                    ((int)(FeedbackAnswerStatuses.Answered | FeedbackAnswerStatuses.Archived), false) => 48,
                    _ => 12
                };
                return new FeedbacksPage { Items = [], Total = total };
            });

        var sut = CreateSut();

        var result = await sut.Index("priority");

        var model = Assert.IsType<FeedbacksPageViewModel>(Assert.IsType<ViewResult>(result).Model);

        Assert.Equal(12, model.CountAll);
        Assert.Equal(3, model.CountPriority);
        Assert.Equal(48, model.CountArchive);

        // Счётчик текущей вкладки совпадает с Total выборки страницы
        Assert.Equal(3, model.Total);
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
