using Api.Conhecimento.Models;
using Api.Conhecimento.Services;
using Api.Conhecimento.Tools;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Api.Conhecimento.Controllers;

/// <summary>
/// Controller para protocolo MCP (Model Context Protocol)
/// Implementa JSON-RPC 2.0 para comunicação com clientes MCP
/// </summary>
[ApiController]
[Route("mcp")]
[Produces("application/json")]
public class McpController : ControllerBase
{
    private readonly McpToolProvider _toolProvider;
    private readonly ILogger<McpController> _logger;

    public McpController(McpToolProvider toolProvider, ILogger<McpController> logger)
    {
        _toolProvider = toolProvider;
        _logger = logger;
    }

    /// <summary>
    /// POST /mcp/rpc - Processa requisições JSON-RPC 2.0
    /// </summary>
    [HttpPost("rpc")]
    [ProducesResponseType(typeof(McpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(McpResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessJsonRpc(
        [FromBody] McpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Requisição MCP recebida: Método={Method}, ID={Id}", request.Method, request.Id);

            var response = new McpResponse
            {
                Id = request.Id
            };

            return request.Method switch
            {
                "tools/list" => Ok(await HandleToolsList(response)),
                "tools/call" => Ok(await HandleToolCall(request, response, cancellationToken)),
                _ => BadRequest(new McpResponse
                {
                    Id = request.Id,
                    Error = new McpError
                    {
                        Code = -32601,
                        Message = $"Método '{request.Method}' não implementado"
                    }
                })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar requisição MCP");

            return Ok(new McpResponse
            {
                Id = request.Id,
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Erro interno do servidor",
                    Data = new { exception = ex.Message }
                }
            });
        }
    }

    /// <summary>
    /// Retorna lista de ferramentas disponíveis
    /// </summary>
    private async Task<McpResponse> HandleToolsList(McpResponse response)
    {
        var tools = _toolProvider.GetAvailableTools();
        response.Result = new { tools };
        return await Task.FromResult(response);
    }

    /// <summary>
    /// Chama uma ferramenta específica
    /// </summary>
    private async Task<McpResponse> HandleToolCall(
        McpRequest request,
        McpResponse response,
        CancellationToken cancellationToken)
    {
        if (request.Params == null || !request.Params.TryGetValue("name", out var toolNameObj))
        {
            response.Error = new McpError
            {
                Code = -32602,
                Message = "Parâmetro 'name' obrigatório em tools/call"
            };
            return response;
        }

        var toolName = toolNameObj?.ToString();
        request.Params.TryGetValue("arguments", out var arguments);

        var toolArguments = arguments switch
        {
            Dictionary<string, object> dictionary => dictionary,
            JsonElement { ValueKind: JsonValueKind.Object } jsonObject =>
                JsonSerializer.Deserialize<Dictionary<string, object>>(jsonObject.GetRawText()),
            _ => null
        };

        try
        {
            var result = await _toolProvider.ExecuteToolAsync(
                toolName!,
                toolArguments,
                cancellationToken
            );

            response.Result = new { content = result };
        }
        catch (Exception ex)
        {
            response.Error = new McpError
            {
                Code = -32603,
                Message = $"Erro ao executar ferramenta: {ex.Message}"
            };
        }

        return response;
    }

    /// <summary>
    /// GET /mcp/health - Verifica saúde do servidor MCP
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            tools = _toolProvider.GetAvailableTools().Count
        });
    }

    /// <summary>
    /// GET /mcp/tools - Lista ferramentas disponíveis (simples REST)
    /// </summary>
    [HttpGet("tools")]
    [ProducesResponseType(typeof(List<McpTool>), StatusCodes.Status200OK)]
    public IActionResult ListTools()
    {
        return Ok(_toolProvider.GetAvailableTools());
    }
}
