using ClientInactiveRedisHandler.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Text;
using System.Text.Json;

namespace ClientInactiveRedisHandler.Services.Implementations
{
    public class ClientInactiveRedisHandlerService : BackgroundService
    {
        private readonly IRedisConnectionPoolManager _redisConnectionPoolManager;
        private readonly IConfiguration _configuration;
        private IConnectionMultiplexer _connection;
        private ISubscriber _subscriber;

        public ClientInactiveRedisHandlerService(
            IRedisConnectionPoolManager redisConnectionPoolManager,
            IConfiguration configuration)
        {
            _redisConnectionPoolManager = redisConnectionPoolManager;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _connection = _redisConnectionPoolManager.GetConnection();
            _subscriber = _connection.GetSubscriber();

            var channel = new RedisChannel("__keyevent@0__:expired", RedisChannel.PatternMode.Auto);
            await _subscriber.SubscribeAsync(channel, _HandleExpiredClient);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _subscriber.UnsubscribeAll();
            await _connection.CloseAsync();
        }

        private void _HandleExpiredClient(RedisChannel channel, RedisValue clientId)
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_configuration.GetSection("RabbitMq")["ConnectionUri"])
            };

            using (var connection = factory.CreateConnection())
            {
                using (var model = connection.CreateModel())
                {

                    model.ExchangeDeclare("client_expired_notification", ExchangeType.Fanout);

                    var body = JsonSerializer.Serialize(new ExpiredClientModel
                    {
                        ClientId = Guid.Parse(clientId.ToString())
                    });

                    model.BasicPublish("client_expired_notification", "", true, null, Encoding.UTF8.GetBytes(body));

                    Console.WriteLine($"EXPIRED: {clientId}");
                }
            }
        }
    }
}
