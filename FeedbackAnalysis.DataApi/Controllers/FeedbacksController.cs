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
            await _feedbacksService.AddNewFeedbacks(model.Feedbacks);

            return Ok();
        }
    }
}
