using System.Text.Json.Serialization;

namespace Api.Conhecimento.Models;

/// <summary>
/// Representa uma Tool disponível no servidor MCP
/// </summary>
public class McpTool
{
    /// <summary>
    /// Nome único da tool
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Descrição da funcionalidade da tool
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    /// <summary>
    /// Schema JSON dos parâmetros de entrada
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public required Dictionary<string, object> InputSchema { get; set; }
}
