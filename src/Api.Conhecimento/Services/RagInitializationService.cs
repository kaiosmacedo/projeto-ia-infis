namespace Api.Conhecimento.Services;

public sealed class RagInitializationService : BackgroundService
{
    private static readonly TimeSpan InitializationTimeout = TimeSpan.FromSeconds(60);

    private readonly DocumentLoaderService _documentLoader;
    private readonly DocumentChunkingService _chunking;
    private readonly OpenAiEmbeddingService _embedding;
    private readonly VectorStoreService _vectorStore;
    private readonly ILogger<RagInitializationService> _logger;

    public RagInitializationService(
        DocumentLoaderService documentLoader,
        DocumentChunkingService chunking,
        OpenAiEmbeddingService embedding,
        VectorStoreService vectorStore,
        ILogger<RagInitializationService> logger)
    {
        _documentLoader = documentLoader;
        _chunking = chunking;
        _embedding = embedding;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutSource.CancelAfter(InitializationTimeout);

        try
        {
            _logger.LogInformation("Inicializando sistema RAG...");
            var documents = await _documentLoader.LoadDocumentsAsync();

            if (documents.Count == 0)
            {
                _logger.LogWarning("Nenhum documento encontrado na pasta Documentos");
                return;
            }

            var chunks = _chunking.ChunkDocuments(documents);
            var embeddings = await _embedding.GetEmbeddingsAsync(chunks, timeoutSource.Token);
            _vectorStore.AddEmbeddings(embeddings);

            var stats = _vectorStore.GetStatistics();
            _logger.LogInformation("Sistema RAG inicializado. Estatísticas: {Stats}",
                System.Text.Json.JsonSerializer.Serialize(stats));
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogError("Inicialização RAG excedeu o limite de {TimeoutSeconds} segundos",
                InitializationTimeout.TotalSeconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Inicialização RAG cancelada durante o encerramento da aplicação");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar sistema RAG");
        }
    }
}