using Api.Conhecimento.Services;
using Api.Conhecimento.Tools;

var builder = WebApplication.CreateBuilder(args);

// Registrar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar serviços de RAG
builder.Services.AddSingleton<DocumentLoaderService>();
builder.Services.AddSingleton<DocumentChunkingService>();
builder.Services.AddSingleton<OpenAiEmbeddingService>();
builder.Services.AddSingleton<VectorStoreService>();
builder.Services.AddSingleton<SimilarityService>();
builder.Services.AddHostedService<RagInitializationService>();

// Registrar MCP Tool Provider com RAG
builder.Services.AddSingleton<McpToolProvider>(sp =>
{
    var provider = new McpToolProvider();
    var tool = new BuscarConhecimentoTool(
        sp.GetRequiredService<DocumentLoaderService>(),
        sp.GetRequiredService<DocumentChunkingService>(),
        sp.GetRequiredService<OpenAiEmbeddingService>(),
        sp.GetRequiredService<VectorStoreService>(),
        sp.GetRequiredService<SimilarityService>(),
        sp.GetRequiredService<ILogger<BuscarConhecimentoTool>>()
    );
    provider.RegisterTool(tool);
    return provider;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Log de inicialização
app.Logger.LogInformation("🚀 API.Conhecimento iniciada como MCP Server HTTP com RAG");

app.Run();
