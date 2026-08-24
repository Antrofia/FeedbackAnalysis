using FeedbackAnalysis.FakeFeedbacksService.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackAnalysis.FakeFeedbacksService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenerationController : ControllerBase
    {
        private const int MaxBatchSize = 200;

        private readonly IFakeFeedbacksGenerator _generator;
        private readonly IFeedbacksPublisher _publisher;
        private readonly IConfiguration _configuration;

        public GenerationController(IFakeFeedbacksGenerator generator, IFeedbacksPublisher publisher, IConfiguration configuration)
        {
            _generator = generator;
            _publisher = publisher;
            _configuration = configuration;
        }

        [Route("run")]
        [HttpPost]
        public async Task<IActionResult> Run([FromQuery] int? count)
        {
            var defaultBatchSize = _configuration.GetSection("Generator").GetValue("BatchSize", 10);
            var batchSize = Math.Clamp(count ?? defaultBatchSize, 1, MaxBatchSize);

            var batch = _generator.GenerateBatch(batchSize);

            await _publisher.PublishAsync(batch);

            return Ok(new { generated = batch.Count });
        }
    }
}
