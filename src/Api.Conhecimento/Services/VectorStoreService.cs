namespace Api.Conhecimento.Services;

/// <summary>
/// Armazenamento em memória de embeddings de documentos
/// </summary>
public class VectorStoreService
{
    private readonly ILogger<VectorStoreService> _logger;
    private readonly List<EmbeddingData> _vectorStore;
    private readonly object _lockObject = new();

    public VectorStoreService(ILogger<VectorStoreService> logger)
    {
        _logger = logger;
        _vectorStore = new List<EmbeddingData>();
        _logger.LogInformation("💾 Vector Store inicializado");
    }

    /// <summary>
    /// Quantidade de embeddings armazenados
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lockObject)
            {
                return _vectorStore.Count;
            }
        }
    }

    /// <summary>
    /// Adiciona embeddings ao armazenamento
    /// </summary>
    public void AddEmbeddings(List<EmbeddingData> embeddings)
    {
        lock (_lockObject)
        {
            _vectorStore.AddRange(embeddings);
            _logger.LogInformation("➕ {Count} embedding(s) adicionado(s) ao Vector Store (Total: {Total})",
                embeddings.Count, _vectorStore.Count);
        }
    }

    /// <summary>
    /// Limpa o armazenamento
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _vectorStore.Clear();
            _logger.LogInformation("🗑️ Vector Store limpo");
        }
    }

    /// <summary>
    /// Retorna todos os embeddings
    /// </summary>
    public List<EmbeddingData> GetAll()
    {
        lock (_lockObject)
        {
            return new List<EmbeddingData>(_vectorStore);
        }
    }

    /// <summary>
    /// Obtém um embedding específico por ID
    /// </summary>
    public EmbeddingData? GetById(string chunkId)
    {
        lock (_lockObject)
        {
            return _vectorStore.FirstOrDefault(e => e.ChunkId == chunkId);
        }
    }

    /// <summary>
    /// Verifica se o armazenamento tem dados
    /// </summary>
    public bool IsEmpty()
    {
        lock (_lockObject)
        {
            return _vectorStore.Count == 0;
        }
    }

    /// <summary>
    /// Obtém estatísticas do Vector Store
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        lock (_lockObject)
        {
            var byDocument = _vectorStore
                .GroupBy(e => e.SourceDocument)
                .ToDictionary(g => g.Key, g => g.Count());

            return new Dictionary<string, object>
            {
                { "TotalEmbeddings", _vectorStore.Count },
                { "Documents", byDocument },
                { "VectorDimension", _vectorStore.FirstOrDefault()?.Vector.Length ?? 0 }
            };
        }
    }
}
