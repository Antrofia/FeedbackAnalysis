using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace FeedbackAnalysis.DataApi.Services
{
    public class FeedbacksService : IFeedbacksService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FeedbacksService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            foreach (var feedback in items)
            {
                if (existingIds.Contains(feedback.Id))
                {
                    continue;
                }

                await _unitOfWork.FeedbackRepository.AddAsync(feedback);
            }

            await _unitOfWork.SaveAsync();
        }

        public async Task<PagedResult<FeedbackModel>> GetListAsync(DateTime dateFrom, DateTime dateTo, FeedbackAnswerStatuses status = (FeedbackAnswerStatuses)~0, int page = 1, int pageSize = 50)
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

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<FeedbackModel>
            {
                Items = items,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<List<FeedbackModel>> GetAllAsync()
        {
            return await _unitOfWork.FeedbackRepository.GetAll().ToListAsync();
        }
    }
}
