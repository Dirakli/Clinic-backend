using StackExchange.Redis;
using Microsoft.Extensions.Options;

namespace Clinic.Services
{
    public class RedisOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public int DefaultDatabase { get; set; }
    }

    public interface IRedisService
    {
        IDatabase GetDatabase(int? db = null);
    }
    public class RedisService : IRedisService
    {
        private readonly ConnectionMultiplexer _redisConnection;

        public RedisService(IOptions<RedisOptions> options)
        {
            var configurationOptions = new ConfigurationOptions
            {
                EndPoints = { $"{options.Value.Host}:{options.Value.Port}" },
                Password = options.Value.Password,
                DefaultDatabase = options.Value.DefaultDatabase
            };

            _redisConnection = ConnectionMultiplexer.Connect(configurationOptions);
        }

        public IDatabase GetDatabase(int? db = null)
        {
            return _redisConnection.GetDatabase(db ?? -1);  
        }
    }
}
