namespace FeedbackAnalysis.DataApi.Services
{
    public interface ITonalityService
    {
        /// <summary>Классифицирует текст: negative→−confidence, positive→+confidence, neutral→0; ошибка/пустой текст → null.</summary>
        Task<double?> ClassifyAsync(string? text, CancellationToken ct = default);

        /// <summary>
        /// Классифицирует список текстов одним батч-запросом.
        /// Результат выровнен по индексам входа; пустые тексты и ошибки → null (ML не вызывается).
        /// </summary>
        Task<IReadOnlyList<double?>> ClassifyBatchAsync(IReadOnlyCollection<string?> texts, CancellationToken ct = default);
    }
}
