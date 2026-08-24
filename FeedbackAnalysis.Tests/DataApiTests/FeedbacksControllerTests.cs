using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Controllers;
using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FeedbackAnalysis.Tests.DataApiTests;

public class FeedbacksControllerTests
{
    private readonly Mock<IFeedbacksService> _serviceMock = new();

    private FeedbacksController CreateSut() => new(_serviceMock.Object);

    private static string? GetProperty(object payload, string name)
    {
        return payload.GetType().GetProperty(name)?.GetValue(payload)?.ToString();
    }

    private void SetupGetList()
    {
        _serviceMock
            .Setup(s => s.GetListAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<FeedbackAnswerStatuses>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedResult<FeedbackDetailedModel>());
    }

    [Fact]
    public async Task GetList_DateFromIsZero_ReturnsBadRequestWithError()
    {
        var result = await CreateSut().GetList(dateFrom: 0, dateTo: 1000);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("dateFrom", GetProperty(badRequest.Value!, "error"));
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetList_DateToIsNegative_ReturnsBadRequestWithError()
    {
        var result = await CreateSut().GetList(dateFrom: 10, dateTo: -5);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("dateTo", GetProperty(badRequest.Value!, "error"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task GetList_PageLessThanOne_ReturnsBadRequestWithError(int page)
    {
        var result = await CreateSut().GetList(dateFrom: 1, dateTo: 2, page: page);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("page", GetProperty(badRequest.Value!, "error"));
    }

    [Fact]
    public async Task GetList_ValidRequest_ConvertsUnixSecondsToUtcDateTimes()
    {
        DateTime capturedFrom = default;
        DateTime capturedTo = default;

        _serviceMock
            .Setup(s => s.GetListAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<FeedbackAnswerStatuses>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Callback<DateTime, DateTime, FeedbackAnswerStatuses, int, int, bool>((from, to, _, _, _, _) =>
            {
                capturedFrom = from;
                capturedTo = to;
            })
            .ReturnsAsync(new PagedResult<FeedbackDetailedModel>());

        await CreateSut().GetList(dateFrom: 86400, dateTo: 172800);

        Assert.Equal(new DateTime(1970, 1, 2, 0, 0, 0), capturedFrom);
        Assert.Equal(DateTimeKind.Utc, capturedFrom.Kind);
        Assert.Equal(new DateTime(1970, 1, 3, 0, 0, 0), capturedTo);
        Assert.Equal(DateTimeKind.Utc, capturedTo.Kind);
    }

    [Theory]
    [InlineData(500, 200)]
    [InlineData(201, 200)]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(50, 50)]
    [InlineData(1, 1)]
    public async Task GetList_ClampsPageSizeIntoRange(int requestedPageSize, int expectedPageSize)
    {
        int capturedPageSize = 0;

        _serviceMock
            .Setup(s => s.GetListAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<FeedbackAnswerStatuses>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Callback<DateTime, DateTime, FeedbackAnswerStatuses, int, int, bool>((_, _, _, _, pageSize, _) => capturedPageSize = pageSize)
            .ReturnsAsync(new PagedResult<FeedbackDetailedModel>());

        await CreateSut().GetList(dateFrom: 1, dateTo: 2, pageSize: requestedPageSize);

        Assert.Equal(expectedPageSize, capturedPageSize);
    }

    [Fact]
    public async Task GetList_PassesDefaultAllStatusesFlag()
    {
        FeedbackAnswerStatuses capturedStatus = 0;
        SetupCapture((_, _, status, _, _, _) => capturedStatus = status);

        await CreateSut().GetList(dateFrom: 1, dateTo: 2);

        Assert.Equal((int)~0, (int)capturedStatus);
    }

    [Fact]
    public async Task GetList_PriorityFlag_IsPassedToService_DefaultFalse()
    {
        bool capturedPriority = true;
        SetupCapture((_, _, _, _, _, priorityOnly) => capturedPriority = priorityOnly);

        await CreateSut().GetList(dateFrom: 1, dateTo: 2);

        Assert.False(capturedPriority);
    }

    [Fact]
    public async Task GetList_PriorityTrue_IsPassedToService()
    {
        bool capturedPriority = false;
        SetupCapture((_, _, _, _, _, priorityOnly) => capturedPriority = priorityOnly);

        await CreateSut().GetList(dateFrom: 1, dateTo: 2, priority: true);

        Assert.True(capturedPriority);
    }

    private void SetupCapture(Action<DateTime, DateTime, FeedbackAnswerStatuses, int, int, bool> capture)
    {
        _serviceMock
            .Setup(s => s.GetListAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<FeedbackAnswerStatuses>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Callback(capture)
            .ReturnsAsync(new PagedResult<FeedbackDetailedModel>());
    }

    [Fact]
    public async Task GetList_SuccessfulCall_ReturnsOkWithPagedPayload()
    {
        var feedbacks = new List<FeedbackDetailedModel>
        {
            new() { Id = "wb:9", ServiceId = "9", Service = "wb" }
        };

        _serviceMock
            .Setup(s => s.GetListAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<FeedbackAnswerStatuses>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedResult<FeedbackDetailedModel>
            {
                Items = feedbacks,
                Total = 42,
                Page = 3,
                PageSize = 25
            });

        var result = await CreateSut().GetList(dateFrom: 1, dateTo: 2, page: 3, pageSize: 25);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(42, GetProperty(ok.Value!, "total") != null ? int.Parse(GetProperty(ok.Value!, "total")!) : 0);
        Assert.Equal(3, int.Parse(GetProperty(ok.Value!, "page")!));
        Assert.Equal(25, int.Parse(GetProperty(ok.Value!, "pageSize")!));
        Assert.NotNull(GetProperty(ok.Value!, "feedbacks"));
    }

    [Fact]
    public async Task AddNewFeedbacks_CallsServiceWithModelFeedbacks_ReturnsOk()
    {
        var model = new AddNewFeedbacksModel
        {
            Feedbacks = [new FeedbackModel { Service = "wb", ServiceId = "5" }]
        };

        IEnumerable<FeedbackModel>? passed = null;
        _serviceMock
            .Setup(s => s.AddNewFeedbacksAsync(It.IsAny<IEnumerable<FeedbackModel>>()))
            .Callback<IEnumerable<FeedbackModel>>(items => passed = items)
            .Returns(Task.CompletedTask);

        var result = await CreateSut().AddNewFeedbacks(model);

        Assert.IsType<OkResult>(result);
        Assert.NotNull(passed);
        var item = Assert.Single(passed);
        Assert.Equal("5", item.ServiceId);
    }

    // --- Answer / Archive ---

    [Fact]
    public async Task Answer_ServiceReturnsTrue_ReturnsOk()
    {
        var request = new FeedbackAnswerRequest { FeedbackId = "wb:1", Sender = "op", Text = "ответ" };
        FeedbackAnswerRequest? passed = null;

        _serviceMock
            .Setup(s => s.AnswerFeedbackAsync(It.IsAny<FeedbackAnswerRequest>()))
            .Callback<FeedbackAnswerRequest>(r => passed = r)
            .ReturnsAsync(true);

        var result = await CreateSut().Answer(request);

        Assert.IsType<OkResult>(result);
        Assert.Same(request, passed);
    }

    [Fact]
    public async Task Answer_FeedbackNotFound_ReturnsNotFound()
    {
        _serviceMock
            .Setup(s => s.AnswerFeedbackAsync(It.IsAny<FeedbackAnswerRequest>()))
            .ReturnsAsync(false);

        var result = await CreateSut().Answer(new FeedbackAnswerRequest { FeedbackId = "wb:missing" });

        Assert.IsType<NotFoundResult>(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Archive_PassesIdAndMapsServiceResult(bool serviceResult)
    {
        string? passedId = null;

        _serviceMock
            .Setup(s => s.ArchiveFeedbackAsync(It.IsAny<string>()))
            .Callback<string>(id => passedId = id)
            .ReturnsAsync(serviceResult);

        var result = await CreateSut().Archive("wb:77");

        Assert.Equal("wb:77", passedId);
        Assert.IsType(serviceResult ? typeof(OkResult) : typeof(NotFoundResult), result);
    }
}
