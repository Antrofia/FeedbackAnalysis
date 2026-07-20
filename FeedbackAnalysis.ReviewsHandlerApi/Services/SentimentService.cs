using Newtonsoft.Json;
using System.Net.Sockets;
using System.Text;

namespace FeedbackAnalysis.ReviewsHandlerApi.Services
{
    public class SentimentService : ISentimentService, IDisposable
    {
        private readonly string _socketPath;
        private readonly ILogger<SentimentService> _logger;

        public SentimentService(ILogger<SentimentService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _socketPath = configuration["SocketPath"] ?? "/tmp/sentiment_socket.sock";
        }

        public async Task<int?> AnalyzeSentimentAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Пустой текст для анализа");
                return null;
            }

            try
            {
                var client = new UnixDomainSocketEndPoint(_socketPath);

                using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

                await socket.ConnectAsync(client);

                // Создаем запрос в формате JSON
                var request = new { text = text };
                var jsonRequest = JsonConvert.SerializeObject(request);
                var bytes = Encoding.UTF8.GetBytes(jsonRequest);

                // Отправляем запрос
                await socket.SendAsync(bytes, SocketFlags.None);

                // Получаем ответ
                var buffer = new byte[1024];
                var received = await socket.ReceiveAsync(buffer, SocketFlags.None);
                var response = Encoding.UTF8.GetString(buffer, 0, received);

                // Парсим ответ
                var result = JsonConvert.DeserializeObject<SentimentResponse>(response);

                if (result?.Error != null)
                {
                    _logger.LogError($"Ошибка от сервиса: {result.Error}");
                    return null;
                }

                _logger.LogInformation($"Анализ текста: '{text.Substring(0, Math.Min(50, text.Length))}...' -> Label: {result?.Label}");
                return result?.Label;

            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Ошибка подключения к UDS сокету.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при анализе тональности");
                return null;
            }
        }

        public void Dispose()
        {

        }

        public class SentimentResponse
        {
            public int? Label { get; set; }
            public string? Error { get; set; }
        }
    }
}
