using System.Globalization;
using FeedbackAnalysis.Contracts.Models;
using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FeedbackAnalysis.DataApi.Services
{
    public class FeedbacksService : IFeedbacksService
    {
        private const double DefaultNegativeTonalityThreshold = -0.25;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ITonalityService _tonalityService;
        private readonly IConfiguration _configuration;

        public FeedbacksService(IUnitOfWork unitOfWork, ITonalityService tonalityService, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _tonalityService = tonalityService;
            _configuration = configuration;
        }

        public async Task AddNewFeedbacksAsync(IEnumerable<FeedbackModel> feedbacks)
        {
            var items = feedbacks
                .GroupBy(x => $"{x.Service}:{x.ServiceId}")
                .Select(g => g.First())
                .ToList();

            if (items.Count == 0)
            {
                return;
            }

            foreach (var feedback in items)
            {
                feedback.Id = $"{feedback.Service}:{feedback.ServiceId}";
            }

            var ids = items.Select(x => x.Id).ToList();

            var existingIds = _unitOfWork.FeedbackRepository
                .Find(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToHashSet();

            var newItems = items.Where(x => !existingIds.Contains(x.Id)).ToList();

            if (newItems.Count == 0)
            {
                return;
            }

            foreach (var feedback in newItems)
            {
                await _unitOfWork.FeedbackRepository.AddAsync(feedback);
            }

            // Классификация одним батч-запросом; пустые тексты внутри дают null без вызова ML.
            var tonalities = await _tonalityService.ClassifyBatchAsync(newItems.Select(x => x.Text).ToArray());

            for (var i = 0; i < newItems.Count; i++)
            {
                var feedback = newItems[i];

                if (tonalities[i].HasValue)
                {
                    await _unitOfWork.FeedbackTonalityRepository.AddAsync(new FeedbackTonalityModel
                    {
                        FeedbackId = feedback.Id,
                        Tonality = tonalities[i]!.Value
                    });
                }

                await _unitOfWork.FeedbackAnswerStatusRepository.AddAsync(new FeedbackAnswerStatusModel
                {
                    FeedbackId = feedback.Id,
                    StatusId = (int)(FeedbackAnswerStatuses.RequireToAnswer | FeedbackAnswerStatuses.NotHandled)
                });
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task<bool> AnswerFeedbackAsync(FeedbackAnswerRequest request)
        {
            if (!await FeedbackExistsAsync(request.FeedbackId))
            {
                return false;
            }

            var answer = await _unitOfWork.FeedbackAnswerRepository.GetAsync(request.FeedbackId);
            if (answer is null)
            {
                answer = new FeedbackAnswerModel
                {
                    FeedbackId = request.FeedbackId,
                    Sender = request.Sender ?? "",
                    Text = request.Text ?? "",
                    CreatedDate = DateTime.UtcNow
                };
                await _unitOfWork.FeedbackAnswerRepository.AddAsync(answer);
            }
            else
            {
                answer.Sender = request.Sender ?? "";
                answer.Text = request.Text ?? "";
                answer.CreatedDate = DateTime.UtcNow;
            }

            await SetStatusAsync(request.FeedbackId, FeedbackAnswerStatuses.Answered);

            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<bool> ArchiveFeedbackAsync(string feedbackId)
        {
            if (!await FeedbackExistsAsync(feedbackId))
            {
                return false;
            }

            await SetStatusAsync(feedbackId, FeedbackAnswerStatuses.Archived);

            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<PagedResult<FeedbackDetailedModel>> GetListAsync(DateTime dateFrom, DateTime dateTo, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)~0, int page = 1, int pageSize = 50, bool priorityOnly = false)
        {
            var skip = (page - 1) * pageSize;

            IQueryable<FeedbackModel> query;

            if ((int)status == ~0)
            {
                query = _unitOfWork.FeedbackRepository
                    .Find(x => x.CreatedDate >= dateFrom && x.CreatedDate <= dateTo);
            }
            else
            {
                query =
                    from feedback in _unitOfWork.FeedbackRepository.GetAll()
                    join answerStatus in _unitOfWork.FeedbackAnswerStatusRepository.GetAll()
                        on feedback.Id equals answerStatus.FeedbackId
                    where feedback.CreatedDate >= dateFrom
                          && feedback.CreatedDate <= dateTo
                          && ((int)status & answerStatus.StatusId) != 0
                    select feedback;

                query = query.Distinct();
            }

            // Приоритетные: только негативные (порог из конфига), до пагинации — чтобы total был корректным.
            if (priorityOnly)
            {
                var threshold = GetNegativeTonalityThreshold();

                query =
                    from feedback in query
                    join tonality in _unitOfWork.FeedbackTonalityRepository.GetAll()
                        on feedback.Id equals tonality.FeedbackId
                    where tonality.Tonality <= threshold
                    select feedback;
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<FeedbackDetailedModel>
            {
                Items = await EnrichWithDetailsAsync(items),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<FeedbackModel>> GetAllAsync()
        {
            return await _unitOfWork.FeedbackRepository.GetAll().ToListAsync();
        }

        private Task<bool> FeedbackExistsAsync(string feedbackId)
        {
            return _unitOfWork.FeedbackRepository.Find(x => x.Id == feedbackId).AnyAsync();
        }

        /// <summary>Полностью заменяет статус-строку (или создаёт при отсутствии).</summary>
        private async Task SetStatusAsync(string feedbackId, FeedbackAnswerStatuses status)
        {
            var row = await _unitOfWork.FeedbackAnswerStatusRepository.GetAsync(feedbackId);
            if (row is null)
            {
                await _unitOfWork.FeedbackAnswerStatusRepository.AddAsync(new FeedbackAnswerStatusModel
                {
                    FeedbackId = feedbackId,
                    StatusId = (int)status
                });
                return;
            }

            row.StatusId = (int)status;
        }

        private double GetNegativeTonalityThreshold()
        {
            var raw = _configuration.GetSection("Feedbacks")["NegativeTonalityThreshold"];

            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : DefaultNegativeTonalityThreshold;
        }

        /// <summary>Обогащает страницу тональностью/статусом/ответом (null, если строк нет).</summary>
        private async Task<List<FeedbackDetailedModel>> EnrichWithDetailsAsync(List<FeedbackModel> items)
        {
            if (items.Count == 0)
            {
                return [];
            }

            var ids = items.Select(x => x.Id).ToList();

            var tonalities = await _unitOfWork.FeedbackTonalityRepository
                .Find(x => ids.Contains(x.FeedbackId))
                .ToDictionaryAsync(x => x.FeedbackId);

            var statuses = await _unitOfWork.FeedbackAnswerStatusRepository
                .Find(x => ids.Contains(x.FeedbackId))
                .ToDictionaryAsync(x => x.FeedbackId);

            var answers = await _unitOfWork.FeedbackAnswerRepository
                .Find(x => ids.Contains(x.FeedbackId))
                .ToDictionaryAsync(x => x.FeedbackId);

            return items.Select(feedback =>
            {
                answers.TryGetValue(feedback.Id, out var answer);

                return new FeedbackDetailedModel
                {
                    Id = feedback.Id,
                    Service = feedback.Service,
                    ServiceId = feedback.ServiceId,
                    Rating = feedback.Rating,
                    Sender = feedback.Sender,
                    Text = feedback.Text,
                    CreatedDate = feedback.CreatedDate,
                    NomenclatureLink = feedback.NomenclatureLink,
                    Tonality = tonalities.TryGetValue(feedback.Id, out var tonality) ? tonality.Tonality : null,
                    Status = statuses.TryGetValue(feedback.Id, out var statusRow) ? (FeedbackAnswerStatuses)statusRow.StatusId : null,
                    AnswerSender = answer?.Sender,
                    AnswerText = answer?.Text
                };
            }).ToList();
        }
    }
}
