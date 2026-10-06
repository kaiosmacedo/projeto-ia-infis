using Api.Atendimento.Models;
using System.Net.Http.Json;

namespace Api.Atendimento.Services;

/// <summary>
/// Cliente para comunicação com servidor MCP
/// Implementa o protocolo JSON-RPC 2.0
/// </summary>
public class McpClientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<McpClientService> _logger;
    private readonly string _mcpServerUrl;

    public McpClientService(HttpClient httpClient, IConfiguration configuration, ILogger<McpClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mcpServerUrl = configuration["McpServer:Url"] ?? "http://localhost:55974";

        _logger.LogInformation("🔗 McpClientService inicializado com URL: {McpServerUrl}", _mcpServerUrl);
    }

    /// <summary>
    /// Descobre as ferramentas disponíveis no servidor MCP
    /// </summary>
    public async Task<List<McpTool>> DiscoverToolsAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Descobrindo tools no servidor MCP...");

            var response = await _httpClient.GetAsync($"{_mcpServerUrl}/mcp/tools");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Erro ao descobrir tools. Status: {StatusCode}", response.StatusCode);
                return new List<McpTool>();
            }

            var tools = await response.Content.ReadFromJsonAsync<List<McpTool>>();

            _logger.LogInformation("✅ {Count} tool(s) descoberta(s)", tools?.Count ?? 0);

            return tools ?? new List<McpTool>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao descobrir tools: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executa uma ferramenta no servidor MCP via JSON-RPC 2.0
    /// </summary>
    public async Task<object?> CallToolAsync(
        string toolName,
        Dictionary<string, object>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔧 Chamando tool: {ToolName}", toolName);

            var request = new McpRequest
            {
                Id = Guid.NewGuid().ToString(),
                Method = "tools/call",
                Params = new Dictionary<string, object>
                {
                    { "name", toolName },
                    { "arguments", arguments ?? new Dictionary<string, object>() }
                }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_mcpServerUrl}/mcp/rpc")
            {
                Content = JsonContent.Create(request)
            };

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Erro ao chamar tool. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var mcpResponse = await response.Content.ReadFromJsonAsync<McpResponse>(cancellationToken);

            if (mcpResponse?.Error != null)
            {
                _logger.LogError("❌ Erro MCP: {Message} (Código: {Code})", mcpResponse.Error.Message, mcpResponse.Error.Code);
                return null;
            }

            _logger.LogInformation("✅ Tool {ToolName} executada com sucesso", toolName);

            return mcpResponse?.Result;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            _logger.LogError(ex, "❌ Erro ao executar tool {ToolName}: {Message}", toolName, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Verifica a saúde do servidor MCP
    /// </summary>
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            _logger.LogInformation("🏥 Verificando saúde do servidor MCP...");

            var response = await _httpClient.GetAsync($"{_mcpServerUrl}/mcp/health");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ Servidor MCP está saudável");
                return true;
            }

            _logger.LogWarning("⚠️ Servidor MCP não respondeu (Status: {StatusCode})", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao verificar saúde do servidor MCP: {Message}", ex.Message);
            return false;
        }
    }
}
