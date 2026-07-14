using FeedbackAnalysis.WBService.Models;
using Newtonsoft.Json;
using System.Text;

namespace FeedbackAnalysis.WBService.Services
{
    public class FeedbackParser : IFeedbackParser
    {
        private class FeedbackResponse
        {
            public List<FeedbackItem> feedbacks { get; set; }
        }

        private class FeedbackItem
        {
            public string id { get; set; }
            public int nmId { get; set; }
            public string text { get; set; }
            public int productValuation { get; set; }
            public string pros { get; set; }
            public string cons { get; set; }
            public DateTime createdDate { get; set; }
        }

        /// <summary>
        /// Парсит JSON и возвращает список отзывов в модели FeedbackModel.
        /// </summary>
        /// <param name="jsonResponse">Строка JSON с полем "feedbacks"</param>
        /// <returns>Список FeedbackModel</returns>
        public List<FeedbackModel> ParseFeedbacks(string jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return new List<FeedbackModel>();

            // Десериализуем корневой объект
            var root = JsonConvert.DeserializeObject<FeedbackResponse>(jsonResponse);
            if (root?.feedbacks == null)
                return new List<FeedbackModel>();

            var result = new List<FeedbackModel>(root.feedbacks.Count);

            foreach (var item in root.feedbacks)
            {
                // Формируем полный текст: text + pros + cons (если не пустые)
                var fullText = new StringBuilder();
                if (!string.IsNullOrEmpty(item.text))
                    fullText.Append(item.text);
                //if (!string.IsNullOrEmpty(item.pros))
                //{
                //    if (fullText.Length > 0) fullText.Append(' ');
                //    fullText.Append(item.pros);
                //}
                //if (!string.IsNullOrEmpty(item.cons))
                //{
                //    if (fullText.Length > 0) fullText.Append(' ');
                //    fullText.Append(item.cons);
                //}

                var feedback = new FeedbackModel
                {
                    service = "wb",
                    serviceId = item.id,
                    rating = item.productValuation / 5d,
                    text = fullText.Length > 0 ? fullText.ToString() : null,
                    createdDate = item.createdDate,
                    nomenclatureLink = $"www.wildberries.ru/catalog/{item.nmId}/detail.aspx"
                };

                result.Add(feedback);
            }

            return result;
        }
    }
}
