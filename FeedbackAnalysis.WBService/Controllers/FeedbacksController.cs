using FeedbackAnalysis.WBService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FeedbackAnalysis.WBService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbacksController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IFeedbackParser _feedbackParser;

        public FeedbacksController(IFeedbackParser feedbackParser)
        {
            _feedbackParser = feedbackParser;

            _httpClient = new HttpClient();
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

        [Route("fetch")]
        [HttpPost]
        //[Authorize]
        public async Task<IActionResult> Fetch()
        {
            // push all queries to DataApi
            var res = await _httpClient.GetAsync("https://feedback-view-04.wb.ru/feedbacks/v2/369663092");

            var json = await res.Content.ReadAsStringAsync();

            var feedbacks = _feedbackParser.ParseFeedbacks(json);



            return Ok(new
            {
                feedbacks = feedbacks
            });
        }
    }
}
