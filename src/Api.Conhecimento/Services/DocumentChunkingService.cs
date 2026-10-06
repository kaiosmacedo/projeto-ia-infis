namespace Api.Conhecimento.Services;

/// <summary>
/// Modelo para armazenar um chunk de documento
/// </summary>
public record DocumentChunk(
    string Id,
    string SourceDocument,
    string Content,
    int StartPosition,
    int EndPosition,
    int ChunkIndex
);

/// <summary>
/// Serviço para dividir documentos em chunks
/// </summary>
public class DocumentChunkingService
{
    private readonly ILogger<DocumentChunkingService> _logger;
    private const int DefaultChunkSize = 800;
    private const int DefaultChunkOverlap = 100;

    public DocumentChunkingService(ILogger<DocumentChunkingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Divide um documento em chunks com sobreposição
    /// </summary>
    public List<DocumentChunk> ChunkDocument(
        string sourceDocument,
        string content,
        int chunkSize = DefaultChunkSize,
        int overlap = DefaultChunkOverlap)
    {
        var chunks = new List<DocumentChunk>();

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("⚠️ Conteúdo vazio para documento: {Document}", sourceDocument);
            return chunks;
        }

        try
        {
            // Limpar e normalizar conteúdo
            var normalizedContent = NormalizeContent(content);

            var chunkIndex = 0;
            var startPosition = 0;

            while (startPosition < normalizedContent.Length)
            {
                // Calcular fim do chunk
                var endPosition = Math.Min(startPosition + chunkSize, normalizedContent.Length);

                // Se não é o último chunk, encontrar a última quebra de linha para não dividir parágrafos
                if (endPosition < normalizedContent.Length && !char.IsWhiteSpace(normalizedContent[endPosition]))
                {
                    var lastNewline = normalizedContent.LastIndexOf('\n', endPosition);
                    if (lastNewline > startPosition)
                    {
                        endPosition = lastNewline;
                    }
                }

                var chunkContent = normalizedContent[startPosition..endPosition].Trim();

                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    var chunk = new DocumentChunk(
                        Id: $"{sourceDocument}#{chunkIndex}",
                        SourceDocument: sourceDocument,
                        Content: chunkContent,
                        StartPosition: startPosition,
                        EndPosition: endPosition,
                        ChunkIndex: chunkIndex
                    );

                    chunks.Add(chunk);
                    chunkIndex++;
                }

                // Mover para próximo chunk (com sobreposição)
                startPosition = endPosition - overlap;

                if (startPosition <= 0 && endPosition >= normalizedContent.Length)
                {
                    break;
                }
            }

            _logger.LogInformation("✂️ Documento '{Document}' dividido em {Count} chunk(s) (tamanho: {Size})", 
                sourceDocument, chunks.Count, chunkSize);

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao dividir documento: {Document}", sourceDocument);
            return chunks;
        }
    }

    /// <summary>
    /// Divide múltiplos documentos em chunks
    /// </summary>
    public List<DocumentChunk> ChunkDocuments(
        Dictionary<string, string> documents,
        int chunkSize = DefaultChunkSize,
        int overlap = DefaultChunkOverlap)
    {
        var allChunks = new List<DocumentChunk>();

        foreach (var (fileName, content) in documents)
        {
            var chunks = ChunkDocument(fileName, content, chunkSize, overlap);
            allChunks.AddRange(chunks);
        }

        _logger.LogInformation("📦 Total de {Count} chunk(s) gerado(s) de {Documents} documento(s)",
            allChunks.Count, documents.Count);

        return allChunks;
    }

    /// <summary>
    /// Normaliza o conteúdo removendo espaços excessivos
    /// </summary>
    private static string NormalizeContent(string content)
    {
        // Remover múltiplas quebras de linha
        var normalized = System.Text.RegularExpressions.Regex.Replace(content, @"\n\n+", "\n\n");

        // Remover espaços em branco múltiplos
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        // Restaurar quebras de linha
        normalized = normalized.Replace(" \n ", "\n");

        return normalized.Trim();
    }
}
