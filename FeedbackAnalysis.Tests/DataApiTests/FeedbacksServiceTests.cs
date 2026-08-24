using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Repositories.EF;
using FeedbackAnalysis.DataApi.Services;
using FeedbackAnalysis.DataApi.UnitOfWork;
using FeedbackAnalysis.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Globalization;

namespace FeedbackAnalysis.Tests.DataApiTests;

/// <summary>
/// Тесты FeedbacksService на реальной SQLite in-memory базе.
/// </summary>
public class FeedbacksServiceTests : IDisposable
{
    private readonly SqliteTestDb _db = new();
    private readonly EFContext _context;
    private readonly EFUnitOfWork _unitOfWork;
    private readonly Mock<ITonalityService> _tonalityMock = new();
    private readonly FeedbacksService _sut;

    public FeedbacksServiceTests()
    {
        _context = _db.CreateContext();

        _unitOfWork = new EFUnitOfWork(
            _context,
            new FeedbackRepository(_context),
            new FeedbackAnswerStatusRepository(_context),
            new FeedbackTonalityRepository(_context),
            new FeedbackAnswerRepository(_context));

        // Дефолт: ML возвращает null на каждый текст батча — конкретные тесты переопределяют через SetupTonality.
        _tonalityMock
            .Setup(s => s.ClassifyBatchAsync(It.IsAny<IReadOnlyCollection<string?>>(), It.IsAny<CancellationToken>()))
            .Returns(new Func<IReadOnlyCollection<string?>, CancellationToken, Task<IReadOnlyList<double?>>>(
                (texts, _) => Task.FromResult<IReadOnlyList<double?>>(new double?[texts.Count])));

        _sut = CreateSut();
    }

    private FeedbacksService CreateSut(double? negativeThreshold = null)
    {
        var settings = new Dictionary<string, string?>();

        if (negativeThreshold.HasValue)
        {
            settings["Feedbacks:NegativeTonalityThreshold"] = negativeThreshold.Value.ToString(CultureInfo.InvariantCulture);
        }

        return new FeedbacksService(_unitOfWork, _tonalityMock.Object, new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
    }

    private void SetupTonality(params double?[] values)
    {
        _tonalityMock
            .Setup(s => s.ClassifyBatchAsync(It.IsAny<IReadOnlyCollection<string?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(values.ToArray());
    }

    private static FeedbackModel MakeFeedback(string serviceId, DateTime? created = null, double rating = 0.8, string? text = null)
    {
        return new FeedbackModel
        {
            Id = $"wb:{serviceId}",
            Service = "wb",
            ServiceId = serviceId,
            Rating = rating,
            Text = text,
            CreatedDate = created ?? DateTime.UnixEpoch.AddDays(1)
        };
    }

    private async Task SeedAsync(params FeedbackModel[] feedbacks)
    {
        _context.Feedbacks.AddRange(feedbacks);
        await _context.SaveChangesAsync();
    }

    public void Dispose() => _context.Dispose();

    // --- AddNewFeedbacksAsync ---

    [Fact]
    public async Task AddNewFeedbacks_EmptyInput_PersistsNothing()
    {
        await _sut.AddNewFeedbacksAsync([]);

        Assert.Empty(await _sut.GetAllAsync());
    }

    [Fact]
    public async Task AddNewFeedbacks_GeneratesCompositeId()
    {
        await _sut.AddNewFeedbacksAsync([MakeFeedback("42")]);

        var all = await _sut.GetAllAsync();

        var item = Assert.Single(all);
        Assert.Equal("wb:42", item.Id);
    }

    [Fact]
    public async Task AddNewFeedbacks_DuplicatesByServiceAndServiceId_KeepsOnlyFirstOccurrence()
    {
        var first = MakeFeedback("1", text: "first");
        var second = MakeFeedback("1", text: "second");

        await _sut.AddNewFeedbacksAsync(new[] { first, second });

        var all = await _sut.GetAllAsync();

        var item = Assert.Single(all);
        Assert.Equal("first", item.Text);
    }

    [Fact]
    public async Task AddNewFeedbacks_SameServiceIdDifferentServices_AreBothStored()
    {
        var wbItem = MakeFeedback("7");
        wbItem.Service = "wb";
        var ozonItem = MakeFeedback("7");
        ozonItem.Service = "ozon";

        await _sut.AddNewFeedbacksAsync(new[] { wbItem, ozonItem });

        var all = await _sut.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, x => x.Id == "wb:7");
        Assert.Contains(all, x => x.Id == "ozon:7");
    }

    [Fact]
    public async Task AddNewFeedbacks_ExistingIdsAreSkipped_NewOnesAreAdded()
    {
        await SeedAsync(MakeFeedback("exists", rating: 0.4));

        var incomingExisting = MakeFeedback("exists", rating: 0.99, text: "should not overwrite");
        var incomingNew = MakeFeedback("fresh");

        await _sut.AddNewFeedbacksAsync([incomingExisting, incomingNew]);

        var all = await _sut.GetAllAsync();

        Assert.Equal(2, all.Count);

        var existing = all.Single(x => x.ServiceId == "exists");
        Assert.Equal(0.4, existing.Rating);
        Assert.Null(existing.Text);

        Assert.NotNull(all.SingleOrDefault(x => x.ServiceId == "fresh"));
    }

    [Fact]
    public async Task AddNewFeedbacks_NewFeedback_GetsStatusRequireToAnswerAndNotHandled()
    {
        SetupTonality(-0.5);

        await _sut.AddNewFeedbacksAsync([MakeFeedback("42", text: "some text")]);

        var statusRow = Assert.Single(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
        Assert.Equal("wb:42", statusRow.FeedbackId);
        Assert.Equal((int)(FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.NotHandled), statusRow.StatusId);
    }

    [Fact]
    public async Task AddNewFeedbacks_NegativeMlResponse_PersistsNegativeTonalityRow()
    {
        SetupTonality(-0.87);

        await _sut.AddNewFeedbacksAsync([MakeFeedback("42", text: "bad text")]);

        var tonalityRow = Assert.Single(await _unitOfWork.FeedbackTonalityRepository.GetAll().ToListAsync());
        Assert.Equal("wb:42", tonalityRow.FeedbackId);
        Assert.Equal(-0.87, tonalityRow.Tonality);
    }

    [Fact]
    public async Task AddNewFeedbacks_MlReturnsNull_NoTonalityRow_ButStatusExists()
    {
        SetupTonality(new double?[] { null });

        await _sut.AddNewFeedbacksAsync([MakeFeedback("42", text: "any text")]);

        Assert.Empty(await _unitOfWork.FeedbackTonalityRepository.GetAll().ToListAsync());

        var statusRow = Assert.Single(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
        Assert.Equal((int)(FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.NotHandled), statusRow.StatusId);
    }

    [Fact]
    public async Task AddNewFeedbacks_EmptyText_TonalityNotRequested_StatusStillWritten()
    {
        await _sut.AddNewFeedbacksAsync([MakeFeedback("42", text: "")]);

        _tonalityMock.Verify(
            s => s.ClassifyBatchAsync(
                It.Is<IReadOnlyCollection<string?>>(texts => texts.Count == 1 && texts.First() == ""),
                It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Empty(await _unitOfWork.FeedbackTonalityRepository.GetAll().ToListAsync());
        Assert.Single(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
    }

    [Fact]
    public async Task AddNewFeedbacks_MixedEmptyAndFilledTexts_TonalityAlignedByIndex()
    {
        SetupTonality(-0.5, null);

        await _sut.AddNewFeedbacksAsync([
            MakeFeedback("with-text", text: "негативный отзыв"),
            MakeFeedback("empty-text", text: "")
        ]);

        var tonalityRows = await _unitOfWork.FeedbackTonalityRepository.GetAll().ToListAsync();
        var row = Assert.Single(tonalityRows);
        Assert.Equal("wb:with-text", row.FeedbackId);
        Assert.Equal(-0.5, row.Tonality);

        Assert.Equal(2, (await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync()).Count);
    }

    [Fact]
    public async Task AddNewFeedbacks_ExistingIdsAreSkipped_NoStatusOrTonalityRowsForThem()
    {
        await SeedAsync(MakeFeedback("exists"));

        await _sut.AddNewFeedbacksAsync([MakeFeedback("exists")]);

        Assert.Empty(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
        Assert.Empty(await _unitOfWork.FeedbackTonalityRepository.GetAll().ToListAsync());
    }

    // --- AnswerFeedbackAsync / ArchiveFeedbackAsync ---

    [Fact]
    public async Task AnswerFeedbackAsync_CreatesAnswer_AndSetsStatusAnswered()
    {
        await SeedAsync(MakeFeedback("42"));

        var result = await _sut.AnswerFeedbackAsync(new FeedbackAnswerRequest { FeedbackId = "wb:42", Sender = "operator", Text = "ответ" });

        Assert.True(result);

        var answer = Assert.Single(await _unitOfWork.FeedbackAnswerRepository.GetAll().ToListAsync());
        Assert.Equal("operator", answer.Sender);
        Assert.Equal("ответ", answer.Text);

        var statusRow = Assert.Single(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
        Assert.Equal((int)FeedbackAnswerStatuses.Answered, statusRow.StatusId);
    }

    [Fact]
    public async Task AnswerFeedbackAsync_ExistingAnswer_IsUpdatedWithNewValues()
    {
        await SeedAsync(MakeFeedback("42"));
        _context.FeedbacksAnswers.Add(new DataApi.Models.FeedbackAnswerModel
        {
            FeedbackId = "wb:42",
            Sender = "old",
            Text = "old text",
            CreatedDate = DateTime.UnixEpoch
        });
        await _context.SaveChangesAsync();

        var result = await _sut.AnswerFeedbackAsync(new FeedbackAnswerRequest { FeedbackId = "wb:42", Sender = "new", Text = "new text" });

        Assert.True(result);

        var answers = await _unitOfWork.FeedbackAnswerRepository.GetAll().ToListAsync();
        var answer = Assert.Single(answers);
        Assert.Equal("new", answer.Sender);
        Assert.Equal("new text", answer.Text);
        Assert.True(answer.CreatedDate > DateTime.UtcNow.AddMinutes(-1));

        var statusRow = Assert.Single(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
        Assert.Equal((int)FeedbackAnswerStatuses.Answered, statusRow.StatusId);
    }

    [Fact]
    public async Task AnswerFeedbackAsync_MissingFeedback_ReturnsFalseAndPersistsNothing()
    {
        var result = await _sut.AnswerFeedbackAsync(new FeedbackAnswerRequest { FeedbackId = "wb:missing", Sender = "op", Text = "t" });

        Assert.False(result);
        Assert.Empty(await _unitOfWork.FeedbackAnswerRepository.GetAll().ToListAsync());
        Assert.Empty(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
    }

    [Fact]
    public async Task ArchiveFeedbackAsync_ReplacesStatusWithArchived()
    {
        await SeedAsync(MakeFeedback("42"));
        _context.FeedbacksAnswerStatus.Add(new DataApi.Models.FeedbackAnswerStatusModel
        {
            FeedbackId = "wb:42",
            StatusId = (int)(FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.NotHandled)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.ArchiveFeedbackAsync("wb:42");

        Assert.True(result);

        var statusRow = Assert.Single(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
        Assert.Equal((int)FeedbackAnswerStatuses.Archived, statusRow.StatusId);
    }

    [Fact]
    public async Task ArchiveFeedbackAsync_MissingStatusRow_CreatesItWithArchived()
    {
        await SeedAsync(MakeFeedback("42"));

        var result = await _sut.ArchiveFeedbackAsync("wb:42");

        Assert.True(result);

        var statusRow = Assert.Single(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
        Assert.Equal((int)FeedbackAnswerStatuses.Archived, statusRow.StatusId);
    }

    [Fact]
    public async Task ArchiveFeedbackAsync_MissingFeedback_ReturnsFalse()
    {
        var result = await _sut.ArchiveFeedbackAsync("wb:missing");

        Assert.False(result);
        Assert.Empty(await _unitOfWork.FeedbackAnswerStatusRepository.GetAll().ToListAsync());
    }

    // --- GetListAsync ---

    [Fact]
    public async Task GetList_FiltersByDateRange_InclusiveBounds()
    {
        var inside = MakeFeedback("inside", created: new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        var onFromEdge = MakeFeedback("from-edge", created: new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var onToEdge = MakeFeedback("to-edge", created: new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc));
        var before = MakeFeedback("before", created: new DateTime(2024, 5, 31, 23, 59, 59, DateTimeKind.Utc));
        var after = MakeFeedback("after", created: new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc));

        await SeedAsync(inside, onFromEdge, onToEdge, before, after);

        var result = await _sut.GetListAsync(
            new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc));

        Assert.Equal(3, result.Total);
        Assert.Equal(["to-edge", "inside", "from-edge"],
            result.Items.Select(x => x.ServiceId));
    }

    [Fact]
    public async Task GetList_Pagination_ReturnsDescendingSliceWithTotalCount()
    {
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 1; i <= 5; i++)
        {
            await SeedAsync(MakeFeedback($"f{i}", created: baseDate.AddDays(i)));
        }

        var result = await _sut.GetListAsync(baseDate, baseDate.AddYears(1), page: 2, pageSize: 2);

        Assert.Equal(5, result.Total);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(["f3", "f2"], result.Items.Select(x => x.ServiceId));
    }

    [Fact]
    public async Task GetList_AllStatusesBranch_ReturnsFeedbacksWithoutAnswerStatusRows()
    {
        await SeedAsync(MakeFeedback("no-status-row"));

        var result = await _sut.GetListAsync(DateTime.UnixEpoch, DateTime.UnixEpoch.AddYears(50));

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetList_StatusFilter_JoinsAnswerStatusTableByFlag()
    {
        var requireAnswer = MakeFeedback("require-answer");
        var answeredArchived = MakeFeedback("answered-archived");
        var zeroStatus = MakeFeedback("zero-status");

        await SeedAsync(requireAnswer, answeredArchived, zeroStatus);

        _context.FeedbacksAnswerStatus.AddRange(
            new DataApi.Models.FeedbackAnswerStatusModel { FeedbackId = requireAnswer.Id, StatusId = (int)FeedbackAnswerStatuses.RequireToAnswer },
            new DataApi.Models.FeedbackAnswerStatusModel
            {
                FeedbackId = answeredArchived.Id,
                StatusId = (int)(FeedbackAnswerStatuses.Answered | FeedbackAnswerStatuses.Archived)
            },
            new DataApi.Models.FeedbackAnswerStatusModel { FeedbackId = zeroStatus.Id, StatusId = 0 });
        await _context.SaveChangesAsync();

        var requireResult = await _sut.GetListAsync(
            DateTime.UnixEpoch, DateTime.UnixEpoch.AddYears(50), FeedbackAnswerStatuses.RequireToAnswer);

        Assert.Equal(1, requireResult.Total);
        Assert.Equal("require-answer", Assert.Single(requireResult.Items).ServiceId);

        var answeredResult = await _sut.GetListAsync(
            DateTime.UnixEpoch, DateTime.UnixEpoch.AddYears(50), FeedbackAnswerStatuses.Answered);

        Assert.Equal(1, answeredResult.Total);
        Assert.Equal("answered-archived", Assert.Single(answeredResult.Items).ServiceId);

        var notHandledResult = await _sut.GetListAsync(
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddYears(50),
            FeedbackAnswerStatuses.NotHandled | FeedbackAnswerStatuses.RequireToAnswer);

        Assert.Equal(1, notHandledResult.Total);
        Assert.Equal("require-answer", Assert.Single(notHandledResult.Items).ServiceId);
    }

    [Fact]
    public async Task GetList_PriorityOnly_FiltersByNegativeThresholdBeforePagination()
    {
        var strongNegative = MakeFeedback("strong-neg", created: DateTime.UnixEpoch.AddDays(1));
        var weakNegative = MakeFeedback("weak-neg", created: DateTime.UnixEpoch.AddDays(2));
        var noTonality = MakeFeedback("no-tonality", created: DateTime.UnixEpoch.AddDays(3));

        await SeedAsync(strongNegative, weakNegative, noTonality);

        _context.FeedbacksTonality.AddRange(
            new DataApi.Models.FeedbackTonalityModel { FeedbackId = strongNegative.Id, Tonality = -0.9 },
            new DataApi.Models.FeedbackTonalityModel { FeedbackId = weakNegative.Id, Tonality = -0.1 });
        await _context.SaveChangesAsync();

        var sut = CreateSut(negativeThreshold: -0.25);

        var result = await sut.GetListAsync(
            DateTime.UnixEpoch,
            DateTime.UnixEpoch.AddYears(50),
            priorityOnly: true);

        Assert.Equal(1, result.Total);
        var item = Assert.Single(result.Items);
        Assert.Equal("strong-neg", item.ServiceId);
        Assert.Equal(-0.9, item.Tonality);
    }

    [Fact]
    public async Task GetList_PriorityOnly_DefaultThresholdIsMinusQuarter()
    {
        var negative = MakeFeedback("neg", created: DateTime.UnixEpoch.AddDays(1));
        var neutralZero = MakeFeedback("neutral-zero", created: DateTime.UnixEpoch.AddDays(2));

        await SeedAsync(negative, neutralZero);

        _context.FeedbacksTonality.AddRange(
            new DataApi.Models.FeedbackTonalityModel { FeedbackId = negative.Id, Tonality = -0.25 },
            new DataApi.Models.FeedbackTonalityModel { FeedbackId = neutralZero.Id, Tonality = 0 });
        await _context.SaveChangesAsync();

        var result = await _sut.GetListAsync(DateTime.UnixEpoch, DateTime.UnixEpoch.AddYears(50), priorityOnly: true);

        Assert.Equal(1, result.Total);
        Assert.Equal("neg", Assert.Single(result.Items).ServiceId);
    }

    [Fact]
    public async Task GetList_EnrichesItems_AndLeavesNullsWhenRowsMissing()
    {
        var enriched = MakeFeedback("enriched", created: DateTime.UnixEpoch.AddDays(1), text: "text-1");
        enriched.Sender = "author";
        enriched.NomenclatureLink = "link";
        var bare = MakeFeedback("bare", created: DateTime.UnixEpoch.AddDays(2));

        await SeedAsync(enriched, bare);

        _context.FeedbacksTonality.Add(new DataApi.Models.FeedbackTonalityModel { FeedbackId = enriched.Id, Tonality = 0.75 });
        _context.FeedbacksAnswerStatus.Add(new DataApi.Models.FeedbackAnswerStatusModel { FeedbackId = enriched.Id, StatusId = (int)FeedbackAnswerStatuses.Answered });
        _context.FeedbacksAnswers.Add(new DataApi.Models.FeedbackAnswerModel
        {
            FeedbackId = enriched.Id,
            Sender = "operator",
            Text = "ответ оператора",
            CreatedDate = DateTime.UnixEpoch.AddDays(3)
        });
        await _context.SaveChangesAsync();

        var result = await _sut.GetListAsync(DateTime.UnixEpoch, DateTime.UnixEpoch.AddYears(50));

        Assert.Equal(2, result.Total);

        var detailed = result.Items.Single(x => x.ServiceId == "enriched");
        Assert.Equal(0.75, detailed.Tonality);
        Assert.Equal(FeedbackAnswerStatuses.Answered, detailed.Status);
        Assert.Equal("operator", detailed.AnswerSender);
        Assert.Equal("ответ оператора", detailed.AnswerText);

        var bareItem = result.Items.Single(x => x.ServiceId == "bare");
        Assert.Null(bareItem.Tonality);
        Assert.Null(bareItem.Status);
        Assert.Null(bareItem.AnswerSender);
        Assert.Null(bareItem.AnswerText);
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAll_ReturnsAllSeededFeedbacks()
    {
        await SeedAsync(MakeFeedback("a"), MakeFeedback("b"), MakeFeedback("c"));

        var all = await _sut.GetAllAsync();

        Assert.Equal(3, all.Count);
    }
}
