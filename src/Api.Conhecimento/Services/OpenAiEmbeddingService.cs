using System.Net.Http.Json;

namespace Api.Conhecimento.Services;

/// <summary>
/// Modelo para armazenar um embedding
/// </summary>
public record EmbeddingData(
    string ChunkId,
    string Content,
    float[] Vector,
    string SourceDocument
);

/// <summary>
/// Modelo para requisição de embedding da OpenAI
/// </summary>
internal class OpenAiEmbeddingRequest
{
    public object Input { get; set; } = string.Empty;
    public string Model { get; set; } = "text-embedding-3-small";
}

/// <summary>
/// Modelo para resposta de embedding da OpenAI
/// </summary>
internal class OpenAiEmbeddingResponse
{
    public List<OpenAiEmbedding>? Data { get; set; }
}

internal class OpenAiEmbedding
{
    public int Index { get; set; }
    public List<float>? Embedding { get; set; }
}

/// <summary>
/// Serviço para gerar embeddings usando OpenAI
/// </summary>
public class OpenAiEmbeddingService
{
    private readonly ILogger<OpenAiEmbeddingService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string EmbeddingModel = "text-embedding-3-small";
    private const string OpenAiBaseUrl = "https://api.openai.com/v1";

    public OpenAiEmbeddingService(
        ILogger<OpenAiEmbeddingService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        var apiKey = configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("⚠️ OpenAI:ApiKey não configurada. Embeddings desabilitados.");
            _apiKey = string.Empty;
        }
        else
        {
            _apiKey = apiKey;
            _logger.LogInformation("✅ OpenAI Client inicializado com modelo: {Model}", EmbeddingModel);
        }
    }

    /// <summary>
    /// Verifica se o serviço está disponível
    /// </summary>
    public bool IsAvailable => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>
    /// Gera embedding para um texto
    /// </summary>
    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            _logger.LogWarning("⚠️ OpenAI Client não disponível");
            return null;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("⚠️ Texto vazio para embedding");
                return null;
            }

            // Limitar tamanho do texto para evitar erros da API
            var truncatedText = text.Length > 8000 ? text[..8000] : text;

            var request = new OpenAiEmbeddingRequest
            {
                Input = truncatedText,
                Model = EmbeddingModel
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{OpenAiBaseUrl}/embeddings")
            {
                Content = JsonContent.Create(request)
            };

            httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ Erro ao chamar API OpenAI: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var embeddingResponse = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken);

            if (embeddingResponse?.Data == null || embeddingResponse.Data.Count == 0)
            {
                _logger.LogError("❌ Resposta vazia da API de embeddings");
                return null;
            }

            var embedding = embeddingResponse.Data[0].Embedding;
            _logger.LogDebug("✅ Embedding gerado com {Dimensions} dimensões", embedding?.Count ?? 0);

            return embedding?.ToArray();
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogError(ex, "❌ Erro ao gerar embedding: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Gera embeddings para múltiplos chunks
    /// </summary>
    public async Task<List<EmbeddingData>> GetEmbeddingsAsync(
        List<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<EmbeddingData>();

        if (!IsAvailable)
        {
            _logger.LogWarning("⚠️ Serviço de embeddings não disponível; os documentos não serão indexados.");
            return embeddings;
        }

        try
        {
            _logger.LogInformation("🔄 Gerando embeddings para {Count} chunk(s)...", chunks.Count);

            for (var batchStart = 0; batchStart < chunks.Count; batchStart += 100)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = chunks.Skip(batchStart).Take(100).ToList();

                try
                {
                    var request = new OpenAiEmbeddingRequest
                    {
                        Input = batch.Select(chunk => chunk.Content.Length > 8000
                            ? chunk.Content[..8000]
                            : chunk.Content).ToList(),
                        Model = EmbeddingModel
                    };

                    using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{OpenAiBaseUrl}/embeddings")
                    {
                        Content = JsonContent.Create(request)
                    };
                    httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");

                    using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError("Erro OpenAI ao indexar chunks: {StatusCode} - {Error}",
                            response.StatusCode, errorContent);
                        continue;
                    }

                    var embeddingResponse = await response.Content
                        .ReadFromJsonAsync<OpenAiEmbeddingResponse>(cancellationToken);

                    if (embeddingResponse?.Data == null)
                    {
                        _logger.LogError("Resposta vazia da API de embeddings ao indexar chunks");
                        continue;
                    }

                    foreach (var item in embeddingResponse.Data.OrderBy(item => item.Index))
                    {
                        if (item.Index < 0 || item.Index >= batch.Count || item.Embedding == null)
                        {
                            continue;
                        }

                        var chunk = batch[item.Index];
                        embeddings.Add(new EmbeddingData(
                            ChunkId: chunk.Id,
                            Content: chunk.Content,
                            Vector: item.Embedding.ToArray(),
                            SourceDocument: chunk.SourceDocument));
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao gerar embeddings para lote iniciado em {BatchStart}", batchStart);
                }
            }

            _logger.LogInformation("✅ Embeddings gerados: {Processed} de {Total} chunk(s)",
                embeddings.Count, chunks.Count);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogError(ex, "❌ Erro ao gerar embeddings em lote");
        }

        return embeddings;
    }

}
