using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Context;
using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.Repositories.EF;
using FeedbackAnalysis.DataApi.UnitOfWork;
using FeedbackAnalysis.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace FeedbackAnalysis.Tests.DataApiTests;

public class RepositoryTests : IDisposable
{
    private readonly SqliteTestDb _db = new();
    private readonly EFContext _context;

    public RepositoryTests()
    {
        _context = _db.CreateContext();
    }

    public void Dispose() => _context.Dispose();

    private static FeedbackModel MakeFeedback(string id) => new()
    {
        Id = $"wb:{id}",
        Service = "wb",
        ServiceId = id,
        Rating = 0.6,
        Text = $"text-{id}"
    };

    // --- FeedbackRepository ---

    [Fact]
    public async Task FeedbackRepository_AddAsync_ThenSave_PersistsEntity()
    {
        var repository = new FeedbackRepository(_context);

        await repository.AddAsync(MakeFeedback("1"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stored = await repository.GetAsync("wb:1");

        Assert.NotNull(stored);
        Assert.Equal(0.6, stored.Rating);
    }

    [Fact]
    public async Task FeedbackRepository_GetAsync_ReturnsEntityOrNullOrMissingKey()
    {
        var repository = new FeedbackRepository(_context);
        await repository.AddAsync(MakeFeedback("2"));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var found = await repository.GetAsync("wb:2");
        var missing = await repository.GetAsync("wb:does-not-exist");

        Assert.NotNull(found);
        Assert.Equal("text-2", found.Text);
        Assert.Null(missing);
    }

    [Fact]
    public async Task FeedbackRepository_Find_FiltersByPredicate()
    {
        var repository = new FeedbackRepository(_context);
        await repository.AddAsync(MakeFeedback("a"));
        await repository.AddAsync(MakeFeedback("b"));
        await _context.SaveChangesAsync();

        var result = repository.Find(x => x.ServiceId == "b").ToList();

        var item = Assert.Single(result);
        Assert.Equal("b", item.ServiceId);
    }

    [Fact]
    public async Task FeedbackRepository_Update_ChangesPersistedValues()
    {
        var repository = new FeedbackRepository(_context);
        await repository.AddAsync(MakeFeedback("up"));
        await _context.SaveChangesAsync();

        var tracked = await repository.GetAsync("wb:up");
        Assert.NotNull(tracked);
        tracked.Rating = 0.95;
        repository.Update(tracked);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await repository.GetAsync("wb:up");

        Assert.NotNull(reloaded);
        Assert.Equal(0.95, reloaded.Rating);
    }

    [Fact]
    public async Task FeedbackRepository_RemoveAsync_RemovesExisting_AndIsNoOpForMissingKey()
    {
        var repository = new FeedbackRepository(_context);
        await repository.AddAsync(MakeFeedback("del"));
        await _context.SaveChangesAsync();

        await repository.RemoveAsync("wb:missing");
        await _context.SaveChangesAsync();
        Assert.Equal(1, await repository.GetAll().CountAsync());

        await repository.RemoveAsync("wb:del");
        await _context.SaveChangesAsync();

        Assert.Equal(0, await repository.GetAll().CountAsync());
    }

    [Fact]
    public async Task FeedbackTonalityRepository_CrudWorks()
    {
        var repository = new FeedbackTonalityRepository(_context);
        var feedback = MakeFeedback("t");
        await new FeedbackRepository(_context).AddAsync(feedback);
        await repository.AddAsync(new FeedbackTonalityModel { FeedbackId = feedback.Id, Tonality = -0.25 });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await repository.GetAsync(feedback.Id);

        Assert.NotNull(loaded);
        Assert.Equal(-0.25, loaded.Tonality);

        await repository.RemoveAsync(feedback.Id);
        await _context.SaveChangesAsync();

        Assert.Null(await repository.GetAsync(feedback.Id));
    }

    [Fact]
    public async Task FeedbackAnswerStatusRepository_CrudWorks()
    {
        var repository = new FeedbackAnswerStatusRepository(_context);
        var feedback = MakeFeedback("s");
        await new FeedbackRepository(_context).AddAsync(feedback);
        await repository.AddAsync(new FeedbackAnswerStatusModel { FeedbackId = feedback.Id, StatusId = 3 });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await repository.GetAsync(feedback.Id);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded.StatusId);
    }

    [Fact]
    public async Task FeedbackAnswerRepository_CrudWorks()
    {
        var repository = new FeedbackAnswerRepository(_context);
        var feedback = MakeFeedback("ans");
        await new FeedbackRepository(_context).AddAsync(feedback);
        await repository.AddAsync(new FeedbackAnswerModel
        {
            FeedbackId = feedback.Id,
            Sender = "operator",
            Text = "ответ",
            CreatedDate = DateTime.UnixEpoch
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await repository.GetAsync(feedback.Id);

        Assert.NotNull(loaded);
        Assert.Equal("operator", loaded.Sender);
        Assert.Equal("ответ", loaded.Text);
        Assert.Equal(DateTime.UnixEpoch, loaded.CreatedDate);

        await repository.RemoveAsync(feedback.Id);
        await _context.SaveChangesAsync();

        Assert.Null(await repository.GetAsync(feedback.Id));
    }

    // --- EFUnitOfWork ---

    [Fact]
    public async Task UnitOfWork_SaveAsync_PersistsChangesFromRepositories()
    {
        var unitOfWork = new EFUnitOfWork(
            _context,
            new FeedbackRepository(_context),
            new FeedbackAnswerStatusRepository(_context),
            new FeedbackTonalityRepository(_context),
            new FeedbackAnswerRepository(_context));

        await unitOfWork.FeedbackRepository.AddAsync(MakeFeedback("uow"));
        await unitOfWork.SaveAsync();
        _context.ChangeTracker.Clear();

        Assert.Equal(1, await unitOfWork.FeedbackRepository.GetAll().CountAsync());
    }
}
