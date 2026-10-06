using Api.Atendimento.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar HttpClient e MCP Client Service
builder.Services.AddHttpClient<McpClientService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
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
app.Logger.LogInformation("🚀 API.Atendimento iniciada com MCP Client");

app.Run();
