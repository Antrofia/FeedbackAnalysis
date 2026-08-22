using FeedbackAnalysis.DataApi.Models;
using FeedbackAnalysis.DataApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackAnalysis.DataApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private readonly IFeedbacksService _feedbacksService;

        public FeedbacksController(IFeedbacksService feedbacksService)
        {
            this._feedbacksService = feedbacksService;
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
        public async Task<IActionResult> GetList(long dateFrom, long dateTo, int status = ~0)
        {
            if(dateFrom <=0 || dateTo <= 0)
            {
                return BadRequest(new
                {
                    error = $"Params {nameof(dateFrom)}, {nameof(dateTo)} is required!"
                });
            }


            var dFrom = DateTimeOffset.FromUnixTimeSeconds(dateFrom).UtcDateTime;
            var dTo = DateTimeOffset.FromUnixTimeSeconds(dateTo).UtcDateTime;

            var result = await _feedbacksService.GetListAsync(dFrom, dTo, (FeedbackAnswerStatuses)status);
            return Ok(new
            {
                feedbacks = result
            });
        }
    }
}
