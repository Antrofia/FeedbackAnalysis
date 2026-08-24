using FeedbackAnalysis.ClientUI.Models;
using FeedbackAnalysis.ClientUI.Services;
using FeedbackAnalysis.Contracts.Models;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackAnalysis.ClientUI.Controllers
{
    public class HomeController : Controller
    {
        private const int PageSize = 20;

        private readonly IFeedbacksService _feedbacksService;

        public HomeController(IFeedbacksService feedbacksService)
        {
            _feedbacksService = feedbacksService;
        }

        public async Task<IActionResult> Index(string tab = "all", int page = 1)
        {
            tab = NormalizeTab(tab);

            if (page < 1)
            {
                page = 1;
            }

            // Окно выборки: отзывы за последний год
            var windowStart = DateTime.UnixEpoch.AddYears(1);
            var windowEnd = DateTime.UtcNow;

            var currentFilter = GetFilterForTab(tab);
            var currentPageTask = _feedbacksService.GetFeedbacksAsync(
                windowStart, windowEnd, currentFilter.Status, currentFilter.PriorityOnly, page, PageSize);

            // Счётчики остальных вкладок запрашиваем параллельно (достаточно Total — pageSize = 1)
            var counterTasks = AllTabs
                .Where(t => t != tab)
                .Select(async t =>
                {
                    var filter = GetFilterForTab(t);
                    var pageData = await _feedbacksService.GetFeedbacksAsync(
                        windowStart, windowEnd, filter.Status, filter.PriorityOnly, 1, 1);
                    return (Tab: t, pageData.Total);
                })
                .ToArray();

            await Task.WhenAll(new Task[] { currentPageTask }.Concat(counterTasks));

            var counts = counterTasks
                .Select(t => t.Result)
                .ToDictionary(t => t.Tab, t => t.Total);
            counts[tab] = currentPageTask.Result.Total;

            var model = new FeedbacksPageViewModel
            {
                Items = currentPageTask.Result.Items,
                Total = currentPageTask.Result.Total,
                Tab = tab,
                Page = page,
                PageSize = PageSize,
                CountAll = counts.GetValueOrDefault("all"),
                CountPriority = counts.GetValueOrDefault("priority"),
                CountArchive = counts.GetValueOrDefault("archive")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(string feedbackId, string sender, string text, string tab)
        {
            tab = NormalizeTab(tab);

            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["ReplyError"] = "Текст ответа не может быть пустым.";
                return RedirectToAction(nameof(Index), new { tab });
            }

            var sent = await _feedbacksService.SendAnswerAsync(feedbackId ?? "", sender ?? "Администратор", text);

            TempData[sent ? "ReplySuccess" : "ReplyError"] = sent
                ? "Ответ сохранён."
                : "Не удалось сохранить ответ — сервис данных недоступен.";

            return RedirectToAction(nameof(Index), new { tab });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(string feedbackId, string tab)
        {
            tab = NormalizeTab(tab);

            var archived = await _feedbacksService.ArchiveAsync(feedbackId ?? "");

            TempData[archived ? "ReplySuccess" : "ReplyError"] = archived
                ? "Отзыв отправлен в архив."
                : "Не удалось отправить отзыв в архив — сервис данных недоступен.";

            return RedirectToAction(nameof(Index), new { tab });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static readonly string[] AllTabs = ["all", "priority", "archive"];

        private static string NormalizeTab(string? tab) =>
            tab is "all" or "priority" or "archive" ? tab : "all";

        // Вкладка -> фильтр (status, priorityOnly) для DataApi
        private static (int Status, bool PriorityOnly) GetFilterForTab(string tab) => tab switch
        {
            "priority" => ((int)FeedbackAnswerStatuses.RequireToAnswer, true),
            "archive" => ((int)(FeedbackAnswerStatuses.Answered | FeedbackAnswerStatuses.Archived), false),
            _ => ((int)FeedbackAnswerStatuses.RequireToAnswer, false)
        };
    }
}
