using Api.Conhecimento.Models;
using Api.Conhecimento.Tools;

namespace Api.Conhecimento.Services;

/// <summary>
/// Provedor de ferramentas MCP
/// </summary>
public class McpToolProvider
{
    private readonly Dictionary<string, IMcpTool> _tools = new();

    /// <summary>
    /// Registra uma ferramenta MCP
    /// </summary>
    public void RegisterTool(IMcpTool tool)
    {
        var definition = tool.GetToolDefinition();
        _tools[definition.Name] = tool;
    }

    /// <summary>
    /// Retorna todas as ferramentas disponíveis
    /// </summary>
    public List<McpTool> GetAvailableTools()
    {
        return _tools.Values.Select(t => t.GetToolDefinition()).ToList();
    }

    /// <summary>
    /// Executa uma ferramenta pelo nome
    /// </summary>
    public async Task<object?> ExecuteToolAsync(
        string toolName,
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            throw new InvalidOperationException($"Ferramenta '{toolName}' não encontrada.");
        }

        return await tool.ExecuteAsync(parameters, cancellationToken);
    }
}
