using FeedbackAnalysis.WBService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackAnalysis.WBService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private readonly IFeedbacksDataService _feedbacksDataService;

        public FeedbacksController(IFeedbacksDataService feedbacksDataService)
        {
            _feedbacksDataService = feedbacksDataService;
        }

        [Route("update")]
        [HttpPost]
        public async Task<IActionResult> Update()
        {
            await _feedbacksDataService.SendAllFeedbacksAsync();

            return Ok();
        }

        [Route("test")]
        [HttpGet]
        public IActionResult Test()
        {
            return Ok(new
            {
                log = "success"
            });
        }
    }
}
