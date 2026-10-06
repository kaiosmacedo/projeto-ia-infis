using Api.Conhecimento.Models;

namespace Api.Conhecimento.Tools;

/// <summary>
/// Interface para ferramentas MCP
/// </summary>
public interface IMcpTool
{
    /// <summary>
    /// Metadados da ferramenta
    /// </summary>
    McpTool GetToolDefinition();

    /// <summary>
    /// Executa a ferramenta com os parâmetros fornecidos
    /// </summary>
    Task<object?> ExecuteAsync(
        Dictionary<string, object>? parameters,
        CancellationToken cancellationToken = default);
}
