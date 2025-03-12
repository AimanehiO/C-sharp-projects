
using System.Text.Json;
using StackExchange.Redis;

namespace VideoGameAPI.AppCore
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _redisDb;
        public RedisCacheService(IConnectionMultiplexer redis)
        {
             _redisDb = redis.GetDatabase();
        }
        public async Task<T> GetAsync<T>(string key)
        {
            var value = await _redisDb.StringGetAsync(key);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value);
        }

        public async Task RemoveAsync(string key)
        {
            await _redisDb.KeyDeleteAsync(key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _redisDb.StringSetAsync(key, serializedValue, expiry);
        }
    }
}
