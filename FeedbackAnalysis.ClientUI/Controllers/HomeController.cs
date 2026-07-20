using FeedbackAnalysis.ClientUI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Diagnostics;

namespace FeedbackAnalysis.ClientUI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {



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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
