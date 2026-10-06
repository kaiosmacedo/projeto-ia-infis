using Xunit;
using Moq;
using Api.Conhecimento.Services;
using Microsoft.Extensions.Logging;

namespace Api.Conhecimento.Tests.Services;

/// <summary>
/// Testes unitários para o cálculo de similaridade de cosseno
/// </summary>
public class SimilarityServiceTests
{
    private readonly Mock<ILogger<SimilarityService>> _loggerMock;
    private readonly VectorStoreService _vectorStore;
    private readonly SimilarityService _similarityService;

    public SimilarityServiceTests()
    {
        _loggerMock = new Mock<ILogger<SimilarityService>>();
        var vectorStoreLogger = new Mock<ILogger<VectorStoreService>>();
        _vectorStore = new VectorStoreService(vectorStoreLogger.Object);
        _similarityService = new SimilarityService(_loggerMock.Object, _vectorStore);
    }

    [Fact]
    public void CosineSimilarity_WithIdenticalVectors_ReturnsOne()
    {
        // Arrange
        var vector = new float[] { 1f, 0f, 0f, 1f };

        // Act
        var result = _similarityService.CosineSimilarity(vector, vector);

        // Assert
        Assert.Equal(1f, result, precision: 4);
    }

    [Fact]
    public void CosineSimilarity_WithOrthogonalVectors_ReturnsZero()
    {
        // Arrange
        var vectorA = new float[] { 1f, 0f, 0f };
        var vectorB = new float[] { 0f, 1f, 0f };

        // Act
        var result = _similarityService.CosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.Equal(0f, result, precision: 4);
    }

    [Fact]
    public void CosineSimilarity_WithOppositeVectors_ReturnsNegativeOne()
    {
        // Arrange
        var vectorA = new float[] { 1f, 0f, 0f };
        var vectorB = new float[] { -1f, 0f, 0f };

        // Act
        var result = _similarityService.CosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.Equal(-1f, result, precision: 3);
    }

    [Fact]
    public void CosineSimilarity_WithProportionalVectors_ReturnsOne()
    {
        // Arrange - vetores proporciona são colineares
        var vectorA = new float[] { 2f, 4f, 6f };
        var vectorB = new float[] { 1f, 2f, 3f };

        // Act
        var result = _similarityService.CosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.Equal(1f, result, precision: 4);
    }

    [Fact]
    public void CosineSimilarity_WithDifferentMagnitudes_IgnoreMagnitude()
    {
        // Arrange - mesmo ângulo, magnitudes diferentes
        var vectorA = new float[] { 3f, 4f };
        var vectorB = new float[] { 6f, 8f };

        // Act
        var result = _similarityService.CosineSimilarity(vectorA, vectorB);

        // Assert - deve ser 1 pois a direção é a mesma
        Assert.Equal(1f, result, precision: 4);
    }

    [Fact]
    public void CosineSimilarity_WithNullVectors_ReturnsZero()
    {
        // Arrange
        var vector = new float[] { 1f, 2f, 3f };

        // Act & Assert
        Assert.Equal(0f, _similarityService.CosineSimilarity(null!, null!));
        Assert.Equal(0f, _similarityService.CosineSimilarity(vector, null!));
        Assert.Equal(0f, _similarityService.CosineSimilarity(null!, vector));
    }

    [Fact]
    public void CosineSimilarity_WithDifferentLengths_ReturnsZero()
    {
        // Arrange
        var vectorA = new float[] { 1f, 2f, 3f };
        var vectorB = new float[] { 4f, 5f };

        // Act
        var result = _similarityService.CosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void CosineSimilarity_WithZeroVectors_ReturnsZero()
    {
        // Arrange
        var vectorA = new float[] { 0f, 0f, 0f };
        var vectorB = new float[] { 1f, 2f, 3f };

        // Act
        var result = _similarityService.CosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.Equal(0f, result);
    }

    [Fact]
    public void CosineSimilarity_WithPartiallyAlignedVectors_ReturnsPositiveValue()
    {
        // Arrange - vetores parcialmente alinhados
        var vectorA = new float[] { 1f, 1f, 0f };
        var vectorB = new float[] { 1f, 0f, 0f };

        // Act
        var result = _similarityService.CosineSimilarity(vectorA, vectorB);

        // Assert
        Assert.True(result > 0f && result < 1f);
        Assert.Equal(0.7071f, result, precision: 3);
    }

    [Fact]
    public async Task SearchSimilarAsync_WithEmptyVectorStore_ReturnsEmptyList()
    {
        // Arrange
        var query = new float[] { 1f, 0f, 0f };

        // Act
        var result = await _similarityService.SearchSimilarAsync(query, topK: 3);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchSimilarAsync_WithEmbeddings_ReturnsTopResults()
    {
        // Arrange
        var embeddings = new List<EmbeddingData>
        {
            new("chunk1", "documento1.md", new float[] { 1f, 0f, 0f }, "documento1.md"),
            new("chunk2", "Conteúdo 2", new float[] { 0.9f, 0.1f, 0f }, "documento1.md"),
            new("chunk3", "Conteúdo 3", new float[] { 0f, 1f, 0f }, "documento1.md"),
            new("chunk4", "Conteúdo 4", new float[] { 0f, 0f, 1f }, "documento1.md"),
        };

        _vectorStore.AddEmbeddings(embeddings);
        var query = new float[] { 1f, 0f, 0f };

        // Act
        var result = await _similarityService.SearchSimilarAsync(query, topK: 2);

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count <= 2);
        // Primeiro resultado deve ser chunk1 com similaridade ~1
        Assert.Equal("chunk1", result[0].Embedding.ChunkId);
        Assert.Equal(1f, result[0].Score, precision: 4);
        // Segundo resultado deve ser chunk2
        Assert.Equal("chunk2", result[1].Embedding.ChunkId);
    }

    [Fact]
    public async Task SearchSimilarAsync_RespectsTopK()
    {
        // Arrange
        var embeddings = new List<EmbeddingData>
        {
            new("chunk1", "Conteúdo 1", new float[] { 1f, 0f, 0f }, "doc.md"),
            new("chunk2", "Conteúdo 2", new float[] { 0f, 1f, 0f }, "doc.md"),
            new("chunk3", "Conteúdo 3", new float[] { 0f, 0f, 1f }, "doc.md"),
            new("chunk4", "Conteúdo 4", new float[] { 1f, 1f, 0f }, "doc.md"),
            new("chunk5", "Conteúdo 5", new float[] { 1f, 0f, 1f }, "doc.md"),
        };

        _vectorStore.AddEmbeddings(embeddings);
        var query = new float[] { 1f, 0f, 0f };

        // Act
        var result = await _similarityService.SearchSimilarAsync(query, topK: 3);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task SearchSimilarAsync_WithDifferentTopK_ReturnsCorrectCount(int topK)
    {
        // Arrange
        var embeddings = Enumerable.Range(1, 10)
            .Select(i => new EmbeddingData(
                $"chunk{i}",
                $"Conteúdo {i}",
                GenerateRandomVector(),
                "doc.md"
            ))
            .ToList();

        _vectorStore.AddEmbeddings(embeddings);
        var query = new float[] { 1f, 0f, 0f };

        // Act
        var result = await _similarityService.SearchSimilarAsync(query, topK);

        // Assert
        Assert.True(result.Count <= topK);
    }

    [Fact]
    public async Task SearchSimilarAsync_OrdersByScoreDescending()
    {
        // Arrange
        var embeddings = new List<EmbeddingData>
        {
            new("chunk1", "Conteúdo 1", new float[] { 0.5f, 0.5f }, "doc.md"),
            new("chunk2", "Conteúdo 2", new float[] { 0.9f, 0.1f }, "doc.md"),
            new("chunk3", "Conteúdo 3", new float[] { 0.1f, 0.9f }, "doc.md"),
        };

        _vectorStore.AddEmbeddings(embeddings);
        var query = new float[] { 1f, 0f };

        // Act
        var result = await _similarityService.SearchSimilarAsync(query, topK: 3);

        // Assert
        Assert.NotEmpty(result);
        for (int i = 1; i < result.Count; i++)
        {
            Assert.True(result[i - 1].Score >= result[i].Score);
        }
    }

    /// <summary>
    /// Gera um vetor aleatório normalizado
    /// </summary>
    private float[] GenerateRandomVector(int dimension = 3)
    {
        var random = new Random();
        var vector = new float[dimension];

        for (int i = 0; i < dimension; i++)
        {
            vector[i] = (float)random.NextDouble();
        }

        // Normalizar
        var norm = (float)Math.Sqrt(vector.Sum(v => v * v));
        for (int i = 0; i < dimension; i++)
        {
            vector[i] /= norm;
        }

        return vector;
    }
}
