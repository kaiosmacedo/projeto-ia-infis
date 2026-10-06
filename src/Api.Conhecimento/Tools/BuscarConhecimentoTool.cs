using Api.Conhecimento.Models;
using Api.Conhecimento.Services;

namespace Api.Conhecimento.Tools;

/// <summary>
/// Ferramenta MCP para buscar conhecimento com RAG
/// </summary>
public class BuscarConhecimentoTool : IMcpTool
{
    private readonly DocumentLoaderService _documentLoader;
    private readonly DocumentChunkingService _chunking;
    private readonly OpenAiEmbeddingService _embedding;
    private readonly VectorStoreService _vectorStore;
    private readonly SimilarityService _similarity;
    private readonly ILogger<BuscarConhecimentoTool> _logger;

    public BuscarConhecimentoTool(
        DocumentLoaderService documentLoader,
        DocumentChunkingService chunking,
        OpenAiEmbeddingService embedding,
        VectorStoreService vectorStore,
        SimilarityService similarity,
        ILogger<BuscarConhecimentoTool> logger)
    {
        _documentLoader = documentLoader;
        _chunking = chunking;
        _embedding = embedding;
        _vectorStore = vectorStore;
        _similarity = similarity;
        _logger = logger;
    }

    /// <summary>
    /// Retorna os metadados da ferramenta
    /// </summary>
    public McpTool GetToolDefinition()
    {
        return new McpTool
        {
            Name = "BuscarConhecimento",
            Description = "Busca conhecimento no banco de dados vetorial baseado em similaridade semântica usando RAG",
            InputSchema = new Dictionary<string, object>
            {
                {
                    "type", "object"
                },
                {
                    "properties", new Dictionary<string, object>
                    {
                        {
                            "query", new Dictionary<string, object>
                            {
                                { "type", "string" },
                                { "description", "Texto para buscar conhecimento" }
                            }
                        },
                        {
                            "limite", new Dictionary<string, object>
                            {
                                { "type", "integer" },
                                { "description", "Número máximo de resultados (padrão: 3)" },
                                { "default", 3 }
                            }
                        }
                    }
                },
                {
                    "required", new[] { "query" }
                }
            }
        };
    }

    /// <summary>
    /// Executa a busca de conhecimento usando RAG
    /// </summary>
    public async Task<object?> ExecuteAsync(
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (parameters == null || !parameters.TryGetValue("query", out var queryObj))
            {
                return new
                {
                    sucesso = false,
                    mensagem = "Campo 'query' obrigatório",
                    resultado = (object?)null,
                    timestamp = DateTime.UtcNow
                };
            }

            var query = queryObj?.ToString();

            if (string.IsNullOrWhiteSpace(query))
            {
                return new
                {
                    sucesso = false,
                    mensagem = "Query não pode estar vazia",
                    resultado = (object?)null,
                    timestamp = DateTime.UtcNow
                };
            }

            // Extrair limite (padrão: 3)
            var limite = 3;
            if (parameters.TryGetValue("limite", out var limiteObj) && 
                int.TryParse(limiteObj?.ToString(), out var parsedLimite))
            {
                limite = Math.Min(parsedLimite, 10); // Máximo 10
            }

            _logger.LogInformation("🔍 Buscando conhecimento para query: '{Query}' (limite: {Limite})", 
                query, limite);

            // Verificar se Vector Store tem dados
            if (_vectorStore.IsEmpty())
            {
                _logger.LogWarning("⚠️ Vector Store vazio. Sistema de RAG não foi inicializado.");
                return new
                {
                    sucesso = false,
                    mensagem = "Vector Store vazio. Sistema em inicialização.",
                    resultado = (object?)null,
                    timestamp = DateTime.UtcNow
                };
            }

            // Buscar por similaridade
            var results = await _similarity.SearchByTextAsync(query, _embedding, limite, cancellationToken);

            if (results.Count == 0)
            {
                return new
                {
                    sucesso = false,
                    mensagem = "Nenhum conhecimento similar encontrado",
                    resultado = (object?)null,
                    timestamp = DateTime.UtcNow
                };
            }

            // Formatar resposta com top 3 resultados (máximo)
            var topResults = results.Take(3).ToList();

            var conteudoResposta = topResults.Select((r, index) => new
            {
                posicao = index + 1,
                documento = r.Embedding.SourceDocument,
                similaridade = Math.Round(r.Score, 4),
                conteudo = r.Embedding.Content[..Math.Min(500, r.Embedding.Content.Length)] + 
                          (r.Embedding.Content.Length > 500 ? "..." : "")
            }).ToList();

            _logger.LogInformation("✅ {Count} resultado(s) encontrado(s)", topResults.Count);

            return new
            {
                sucesso = true,
                mensagem = $"Encontrados {topResults.Count} resultado(s) similares",
                totalResultados = topResults.Count,
                resultado = conteudoResposta,
                timestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogError(ex, "❌ Erro ao buscar conhecimento");
            return new
            {
                sucesso = false,
                mensagem = $"Erro ao buscar conhecimento: {ex.Message}",
                resultado = (object?)null,
                timestamp = DateTime.UtcNow
            };
        }
    }
}
