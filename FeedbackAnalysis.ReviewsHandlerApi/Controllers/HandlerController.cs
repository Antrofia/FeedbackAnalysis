using FeedbackAnalysis.ReviewsHandlerApi.Models;
using FeedbackAnalysis.ReviewsHandlerApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackAnalysis.ReviewsHandlerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HandlerController : ControllerBase
    {
        private readonly ISentimentService _sentimentService;

        public HandlerController(ISentimentService sentimentService)
        {
            _sentimentService = sentimentService;
        }

        [HttpPost]
        public async Task<IActionResult> Handle(HandlerRequestModel model)
        {
            var result = await _sentimentService.AnalyzeSentimentAsync(model.Text);

            if(result == null)
            {
                return new StatusCodeResult(500);
            }
            else
            {
                return Ok(new HandlerResponseModel
                {
                    Label = result
                });
            }
        }
    }
}
