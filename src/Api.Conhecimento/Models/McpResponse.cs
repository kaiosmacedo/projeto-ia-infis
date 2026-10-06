using System.Text.Json.Serialization;

namespace Api.Conhecimento.Models;

/// <summary>
/// Resposta JSON-RPC 2.0 do servidor MCP
/// </summary>
public class McpResponse
{
    /// <summary>
    /// Versão do JSON-RPC (sempre "2.0")
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// ID da requisição correspondente
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Resultado da execução (sucesso)
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Erro na execução (falha)
    /// </summary>
    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

/// <summary>
/// Representa um erro JSON-RPC
/// </summary>
public class McpError
{
    /// <summary>
    /// Código de erro
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// Mensagem de erro
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// Dados adicionais do erro
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
