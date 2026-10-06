namespace Api.Conhecimento.Services;

/// <summary>
/// Resultado de busca por similaridade
/// </summary>
public record SimilarityResult(
    EmbeddingData Embedding,
    float Score
);

/// <summary>
/// Serviço para calcular similaridade entre vetores usando cosseno
/// </summary>
public class SimilarityService
{
    private readonly ILogger<SimilarityService> _logger;
    private readonly VectorStoreService _vectorStore;

    public SimilarityService(ILogger<SimilarityService> logger, VectorStoreService vectorStore)
    {
        _logger = logger;
        _vectorStore = vectorStore;
    }

    /// <summary>
    /// Calcula similaridade de cosseno entre dois vetores
    /// Fórmula: cos(θ) = (A · B) / (||A|| * ||B||)
    /// </summary>
    public float CosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA == null || vectorB == null)
        {
            return 0f;
        }

        if (vectorA.Length != vectorB.Length)
        {
            _logger.LogWarning("⚠️ Vetores com dimensões diferentes: {DimA} vs {DimB}",
                vectorA.Length, vectorB.Length);
            return 0f;
        }

        if (vectorA.Length == 0)
        {
            return 0f;
        }

        // Calcular produto escalar (dot product)
        double dotProduct = 0;
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
        }

        // Calcular magnitude de A
        double magnitudeA = 0;
        for (int i = 0; i < vectorA.Length; i++)
        {
            magnitudeA += vectorA[i] * vectorA[i];
        }
        magnitudeA = Math.Sqrt(magnitudeA);

        // Calcular magnitude de B
        double magnitudeB = 0;
        for (int i = 0; i < vectorB.Length; i++)
        {
            magnitudeB += vectorB[i] * vectorB[i];
        }
        magnitudeB = Math.Sqrt(magnitudeB);

        // Evitar divisão por zero
        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0f;
        }

        return (float)(dotProduct / (magnitudeA * magnitudeB));
    }

    /// <summary>
    /// Busca os N chunks mais similares ao texto de query
    /// </summary>
    public async Task<List<SimilarityResult>> SearchSimilarAsync(
        float[] queryVector,
        int topK = 3)
    {
        try
        {
            var allEmbeddings = _vectorStore.GetAll();

            if (allEmbeddings.Count == 0)
            {
                _logger.LogWarning("⚠️ Vector Store vazio");
                return new List<SimilarityResult>();
            }

            _logger.LogInformation("🔍 Buscando {TopK} resultado(s) mais similares entre {Total} documento(s)",
                topK, allEmbeddings.Count);

            // Calcular similaridade para todos os embeddings
            var results = new List<SimilarityResult>();

            await Task.Run(() =>
            {
                foreach (var embedding in allEmbeddings)
                {
                    var score = CosineSimilarity(queryVector, embedding.Vector);
                    results.Add(new SimilarityResult(embedding, score));
                }
            });

            // Ordenar por score descendente e pegar top K
            var topResults = results
                .OrderByDescending(r => r.Score)
                .Take(topK)
                .ToList();

            _logger.LogInformation("✅ Top {Count} resultado(s) encontrado(s). Score mais alto: {MaxScore}",
                topResults.Count,
                topResults.FirstOrDefault()?.Score.ToString("F4") ?? "N/A");

            return topResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao buscar similares");
            return new List<SimilarityResult>();
        }
    }

    /// <summary>
    /// Busca por similaridade usando texto (precisa gerar embedding do texto primeiro)
    /// </summary>
    public async Task<List<SimilarityResult>> SearchByTextAsync(
        string query,
        OpenAiEmbeddingService embeddingService,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📝 Buscando por query: '{Query}' (top {TopK})", query, topK);

            // Gerar embedding da query
            var queryVector = await embeddingService.GetEmbeddingAsync(query, cancellationToken);

            if (queryVector == null)
            {
                _logger.LogError("❌ Falha ao gerar embedding da query");
                return new List<SimilarityResult>();
            }

            return await SearchSimilarAsync(queryVector, topK);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogError(ex, "❌ Erro ao buscar por texto");
            return new List<SimilarityResult>();
        }
    }
}
