using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackAnalysis.DataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private const int MaxPageSize = 200;

        private readonly IFeedbacksService _feedbacksService;

        public FeedbacksController(IFeedbacksService feedbacksService)
        {
            _feedbacksService = feedbacksService;
        }

        [Route("add-new")]
        [HttpPost]
        public async Task<IActionResult> AddNewFeedbacks(AddNewFeedbacksModel model)
        {
            await _feedbacksService.AddNewFeedbacksAsync(model.Feedbacks);

            return Ok();
        }

        [Route("list")]
        [HttpGet]
        public async Task<IActionResult> GetList(long dateFrom, long dateTo, int status = ~0, int page = 1, int pageSize = 50)
        {
            if (dateFrom <= 0 || dateTo <= 0)
            {
                return BadRequest(new
                {
                    error = $"Params {nameof(dateFrom)}, {nameof(dateTo)} is required!"
                });
            }

            if (page < 1)
            {
                return BadRequest(new
                {
                    error = $"{nameof(page)} must be greater than 0!"
                });
            }

            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var dFrom = DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime;
            var dTo = DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime;

            var result = await _feedbacksService.GetListAsync(dFrom, dTo, (FeedbackAnswerStatuses)status, page, pageSize);

            return Ok(new
            {
                feedbacks = result.Items,
                total = result.Total,
                page = result.Page,
                pageSize = result.PageSize
            });
        }
    }
}
