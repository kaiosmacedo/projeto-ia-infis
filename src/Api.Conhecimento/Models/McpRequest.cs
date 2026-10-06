using System.Text.Json.Serialization;

namespace Api.Conhecimento.Models;

/// <summary>
/// Requisição JSON-RPC 2.0 para o servidor MCP
/// </summary>
public class McpRequest
{
    /// <summary>
    /// Versão do JSON-RPC (sempre "2.0")
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// ID único da requisição
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Nome do método a ser chamado
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; set; }

    /// <summary>
    /// Parâmetros do método
    /// </summary>
    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }
}
