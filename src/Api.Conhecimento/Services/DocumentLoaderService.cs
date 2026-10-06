namespace Api.Conhecimento.Services;

/// <summary>
/// Serviço para carregar documentos .md do diretório de documentos
/// </summary>
public class DocumentLoaderService
{
    private readonly ILogger<DocumentLoaderService> _logger;
    private readonly string _documentsPath;

    public DocumentLoaderService(ILogger<DocumentLoaderService> logger)
    {
        _logger = logger;

        // Caminho relativo para a pasta Documentos
        var baseDirectory = AppContext.BaseDirectory;
        _documentsPath = Path.Combine(baseDirectory, "Documentos");

        _logger.LogInformation("📂 Diretório de documentos: {Path}", _documentsPath);
    }

    /// <summary>
    /// Carrega todos os arquivos .md da pasta de documentos
    /// </summary>
    public async Task<Dictionary<string, string>> LoadDocumentsAsync()
    {
        var documents = new Dictionary<string, string>();

        try
        {
            if (!Directory.Exists(_documentsPath))
            {
                _logger.LogWarning("⚠️ Diretório de documentos não encontrado: {Path}", _documentsPath);
                return documents;
            }

            var markdownFiles = Directory.GetFiles(_documentsPath, "*.md");
            _logger.LogInformation("📄 Encontrados {Count} arquivo(s) .md", markdownFiles.Length);

            foreach (var filePath in markdownFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(filePath);
                    var content = await File.ReadAllTextAsync(filePath);

                    documents[fileName] = content;
                    _logger.LogInformation("✅ Documento carregado: {FileName} ({Bytes} bytes)", 
                        fileName, content.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erro ao carregar arquivo: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("📚 Total de {Count} documento(s) carregado(s)", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao carregar documentos");
        }

        return documents;
    }

    /// <summary>
    /// Carrega um documento específico por nome
    /// </summary>
    public async Task<string?> LoadDocumentAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_documentsPath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("⚠️ Arquivo não encontrado: {FileName}", fileName);
                return null;
            }

            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao carregar documento {FileName}", fileName);
            return null;
        }
    }
}
