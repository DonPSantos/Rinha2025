using Polly;
using Polly.CircuitBreaker;
using Rinha2025.Clients;
using Rinha2025.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.AddStackExchangeRedisCache(opt =>
//{
//    opt.Configuration = "localhost:6379";
//    opt.InstanceName = "RinhaRedis";
//});

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379,abortConnect=false")
);

builder.Services.AddHttpClient<PaymentProcessorDefaultClient>(client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PROCESSOR_DEFAULT_URL"));
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<PaymentProcessorFallbackClient>(client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("PROCESSOR_FALLBACK_URL"));
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddResiliencePipeline("circuit-pipeline", builder =>
{
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        BreakDuration = TimeSpan.FromSeconds(5),
        MinimumThroughput = 2,
        ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
    });
});

builder.Services.AddHealthChecks();
builder.Services.AddHostedService<PaymentWorker>();

var app = builder.Build();

var redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
var db = redis.GetDatabase();

// Verificar se o consumer group já existe antes de criar
var streamInfo = await db.StreamInfoAsync("fila-pagamentos");
if (streamInfo.ConsumerGroupCount > 0)
{
    var groupInfo = await db.StreamGroupInfoAsync("fila-pagamentos");
    var groupExists = groupInfo.Any(g => g.Name == "processadores");

    if (!groupExists)
    {
        await db.StreamCreateConsumerGroupAsync("fila-pagamentos", "processadores", "0-0");
    }
}
else
{
    await db.StreamCreateConsumerGroupAsync("fila-pagamentos", "processadores", "0-0", createStream: true);
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();
//}

//app.UseHttpsRedirection();
app.MapHealthChecks("/healthz");

app.UseAuthorization();

app.MapControllers();

app.Run();