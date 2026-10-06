using Api.Atendimento.Models;
using Api.Atendimento.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Atendimento.Controllers;

/// <summary>
/// Controller para testes do cliente MCP
/// </summary>
[ApiController]
[Route("api/test")]
[Produces("application/json")]
public class TestMcpController : ControllerBase
{
    private readonly McpClientService _mcpClient;
    private readonly ILogger<TestMcpController> _logger;

    public TestMcpController(McpClientService mcpClient, ILogger<TestMcpController> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/test/health - Verifica saúde do servidor MCP
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckMcpHealth()
    {
        var isHealthy = await _mcpClient.CheckHealthAsync();

        if (isHealthy)
        {
            return Ok(new { status = "MCP Server is healthy", timestamp = DateTime.UtcNow });
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, 
            new { status = "MCP Server is unavailable", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// GET /api/test/discover - Descobre tools disponíveis no servidor MCP
    /// </summary>
    [HttpGet("discover")]
    [ProducesResponseType(typeof(DiscoverToolsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DiscoverTools()
    {
        try
        {
            _logger.LogInformation("📋 Endpoint /api/test/discover chamado");

            var tools = await _mcpClient.DiscoverToolsAsync();

            return Ok(new DiscoverToolsResponse
            {
                Success = true,
                ToolCount = tools.Count,
                Tools = tools,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao descobrir tools");

            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse
                {
                    Success = false,
                    Message = "Erro ao descobrir tools",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
        }
    }

    /// <summary>
    /// POST /api/test/call-tool - Executa uma ferramenta no servidor MCP
    /// </summary>
    [HttpPost("call-tool")]
    [ProducesResponseType(typeof(CallToolResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CallTool(
        [FromBody] CallToolRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ToolName))
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Campo 'toolName' é obrigatório",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("🔧 Endpoint /api/test/call-tool chamado para: {ToolName}", request.ToolName);

            var result = await _mcpClient.CallToolAsync(request.ToolName, request.Arguments, cancellationToken);

            return Ok(new CallToolResponse
            {
                Success = true,
                ToolName = request.ToolName,
                Result = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao chamar tool");

            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            if (ex is TaskCanceledException)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    new ErrorResponse
                    {
                        Success = false,
                        Message = "Tempo limite excedido ao chamar a ferramenta MCP",
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse
                {
                    Success = false,
                    Message = "Erro ao executar tool",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
        }
    }

    /// <summary>
    /// POST /api/test/buscar-conhecimento - Teste específico da tool BuscarConhecimento
    /// </summary>
    [HttpPost("buscar-conhecimento")]
    [ProducesResponseType(typeof(BuscarConhecimentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BuscarConhecimento(
        [FromBody] BuscarConhecimentoRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new ErrorResponse
                {
                    Success = false,
                    Message = "Campo 'query' é obrigatório",
                    Timestamp = DateTime.UtcNow
                });
            }

            _logger.LogInformation("🔍 Buscando conhecimento: {Query}", request.Query);

            var arguments = new Dictionary<string, object>
            {
                { "query", request.Query },
                { "limite", request.Limite ?? 5 }
            };

            var result = await _mcpClient.CallToolAsync("BuscarConhecimento", arguments, cancellationToken);

            return Ok(new BuscarConhecimentoResponse
            {
                Success = true,
                Query = request.Query,
                Limite = request.Limite ?? 5,
                Result = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao buscar conhecimento");

            if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            if (ex is TaskCanceledException)
            {
                return StatusCode(StatusCodes.Status504GatewayTimeout,
                    new ErrorResponse
                    {
                        Success = false,
                        Message = "Tempo limite excedido ao buscar conhecimento",
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
            }

            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ErrorResponse
                {
                    Success = false,
                    Message = "Erro ao buscar conhecimento",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
        }
    }
}

/// <summary>
/// Request para descobrir tools
/// </summary>
public class DiscoverToolsResponse
{
    public bool Success { get; set; }
    public int ToolCount { get; set; }
    public List<McpTool> Tools { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request para chamar uma tool
/// </summary>
public class CallToolRequest
{
    public required string ToolName { get; set; }
    public Dictionary<string, object>? Arguments { get; set; }
}

/// <summary>
/// Response de chamada de tool
/// </summary>
public class CallToolResponse
{
    public bool Success { get; set; }
    public string? ToolName { get; set; }
    public object? Result { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Request para buscar conhecimento
/// </summary>
public class BuscarConhecimentoRequest
{
    public required string Query { get; set; }
    public int? Limite { get; set; }
}

/// <summary>
/// Response de busca de conhecimento
/// </summary>
public class BuscarConhecimentoResponse
{
    public bool Success { get; set; }
    public string? Query { get; set; }
    public int Limite { get; set; }
    public object? Result { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Response de erro
/// </summary>
public class ErrorResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}
