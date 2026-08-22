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
            var feedbacks = await _feedbacksService.GetFeedbacksAsync(DateTime.UnixEpoch.AddYears(1), DateTime.UtcNow);

            var viewFeedbacks = _mapper.Map<List<FeedbackViewModel>>(feedbacks);

            return View(viewFeedbacks);

            return View(new List<FeedbackModel>
            {
                new FeedbackModel
                {
                    Id = "1",
                    Sender = "Алексей Иванов",
                    Text = "Отличный сервис! Всё быстро и качественно.",
                    Rating = 5,
                    CreatedDate = DateTime.Now.AddDays(-2),
                    NomenclatureLink = "https://www.youtube.com/watch?v=U_fHtq4F1to"
                },
                new FeedbackModel
                {
                    Id = "2",
                    Sender = "Мария Петрова",
                    Text = "Хорошо, но есть над чем поработать.",
                    Rating = 4,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    NomenclatureLink = "https://www.youtube.com/watch?v=U_fHtq4F1to"
                },
                new FeedbackModel
                {
                    Id = "3",
                    Sender = "Иван Сидоров",
                    Text = "Не очень доволен качеством обслуживания.",
                    Rating = 2,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    NomenclatureLink = "https://www.youtube.com/watch?v=U_fHtq4F1to"
        }
            });
        }
    }
}
