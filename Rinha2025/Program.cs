using Polly;
using Polly.CircuitBreaker;
using Rinha2025.Clients;
using Rinha2025.DTO;
using Rinha2025.Services;
using StackExchange.Redis;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("REDIS_CONNECTION")));
builder.Services.AddSingleton<IDatabase>(provider =>
{
    var multiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    return multiplexer.GetDatabase();
});

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

var cbopt = new BoundedChannelOptions(8000)
{
    FullMode = BoundedChannelFullMode.DropNewest,
    SingleReader = false,
    SingleWriter = false
};

var channel = Channel.CreateBounded<ProcessorRequest>(cbopt);
builder.Services.AddSingleton(channel); // Registra a instância do canal
builder.Services.AddSingleton<ChannelReader<ProcessorRequest>>(provider => provider.GetRequiredService<Channel<ProcessorRequest>>().Reader);
builder.Services.AddSingleton<ChannelWriter<ProcessorRequest>>(provider => provider.GetRequiredService<Channel<ProcessorRequest>>().Writer);

builder.Services.AddHostedService<PaymentWorker>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapOpenApi();

app.MapHealthChecks("/healthz");

app.UseAuthorization();

app.MapControllers();

app.Run();