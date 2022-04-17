using ClientInactiveRedisHandler.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis.Extensions.Core;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;
using StackExchange.Redis.Extensions.Newtonsoft;

var builder = Host.CreateDefaultBuilder(args);
var configuration = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddCommandLine(args)
        .AddJsonFile("appsettings.json")
        .Build();

await builder
    .ConfigureAppConfiguration(app =>
    {
        app.Sources.Clear();
        app.AddConfiguration(configuration);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(x => new RedisConfiguration()
        {
            ConnectionString = configuration.GetSection("Redis")["ConnectionString"]
        });
        services.AddSingleton<ISerializer, NewtonsoftSerializer>();
        services.AddSingleton<IRedisConnectionPoolManager, RedisConnectionPoolManager>();
        services.AddScoped<IRedisClient, RedisClient>();

        services.AddHostedService<ClientInactiveRedisHandlerService>();
    })
    .RunConsoleAsync();