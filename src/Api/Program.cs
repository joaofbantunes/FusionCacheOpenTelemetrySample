using Microsoft.Extensions.Caching.Memory;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ZiggyCreatures.Caching.Fusion;

var builder = WebApplication.CreateBuilder(args);

void ConfigureResource(ResourceBuilder resourceBuilder)
{
    resourceBuilder.AddService(
        serviceName: "api",
        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
        serviceInstanceId: Environment.MachineName);
}

builder.Logging.AddOpenTelemetry(o =>
{
    var resourceBuilder = ResourceBuilder.CreateDefault();
    ConfigureResource(resourceBuilder);
    o.SetResourceBuilder(resourceBuilder);
    o.IncludeScopes = true;
    o.IncludeFormattedMessage = true;
    o.ParseStateValues = true;
    o.AddOtlpExporter(options => { options.Endpoint = new Uri("http://localhost:4317"); });
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(ConfigureResource)
    .WithTracing(traceBuilder => traceBuilder
        .AddAspNetCoreInstrumentation()
        .AddFusionCacheInstrumentation(o => o.IncludeMemoryLevel = true)
        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317")))
    .WithMetrics(traceBuilder => traceBuilder
        .AddAspNetCoreInstrumentation()
        .AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2, 2.5, 5, 7.5, 10]
        })
        .AddFusionCacheInstrumentation(o => o.IncludeMemoryLevel = true)
        .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317")));

builder.Services
    .AddFusionCache("SampleCache")
    .WithOptions(o =>
    {
        // nothing to tweak here now, but there are a bunch of interesting options to play with
    })
    // if we didn't provide anything, it would create one, but I wanted to limit the cache size 
    .WithMemoryCache(new MemoryCache(new MemoryCacheOptions
    {
        SizeLimit = 20
    }));

var app = builder.Build();

app.MapGet("/{key}", async (string key, IFusionCacheProvider cacheProvider) =>
{
    var cache = cacheProvider.GetCache("SampleCache");
    var value = await cache.GetOrSetAsync(
        "root_" + key,
        async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
            return Guid.NewGuid();
        },
        new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromSeconds(3),
            Size = 1,
            EagerRefreshThreshold = 0.5f,
            JitterMaxDuration = TimeSpan.FromSeconds(1)
        });
    
    return value;
});

app.Run();