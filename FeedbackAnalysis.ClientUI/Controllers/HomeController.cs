using AutoMapper;
using FeedbackAnalysis.ClientUI.Models;
using FeedbackAnalysis.ClientUI.Services;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackAnalysis.ClientUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFeedbacksService _feedbacksService;
        private readonly IMapper _mapper;

        public HomeController(IFeedbacksService feedbacksService, IMapper mapper)
        {
            _feedbacksService = feedbacksService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var feedbacks = await _feedbacksService.GetFeedbacksAsync(DateTime.UnixEpoch.AddYears(1), DateTime.UtcNow, pageSize: 50);

            var viewFeedbacks = _mapper.Map<List<FeedbackViewModel>>(feedbacks);

            return View(viewFeedbacks);
        }
    }
}
