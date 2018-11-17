using V21Bot.Redis.Serialize;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Redis
{
    public class StackExchangeClient : IRedisClient
    {
        private ConnectionMultiplexer redis;
        private IDatabase database;
        public StackExchangeClient(string host, int db)
        {
            redis = ConnectionMultiplexer.Connect(host);
            database = redis.GetDatabase(db);
        }

		/// <summary>
		/// Initializes the client
		/// </summary>
		/// <returns></returns>
		public async Task Initialize() => await Task.Delay(0);

        public async Task<bool> RemoveAsync(string key)
        {
            return await database.KeyDeleteAsync(key);
        }

        public async Task StringSetAsync(string key, string value, TimeSpan? TTL = null)
		{
			await database.StringSetAsync(key, value, expiry: TTL);
		}
		public async Task<string> StringGetAsync(string key, string @default = null)
		{
			var value = await database.StringGetAsync(key);
			return value.HasValue ? value.ToString() : @default;
		}
		
		/// <summary>
		/// Sets a entire hash
		/// </summary>
		/// <param name="key"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public async Task HashSetAsync(string key, Dictionary<string, string> values)
        {
            await database.HashSetAsync(key, ConvertDictionary(values));
        }

		/// <summary>
		/// Gets a entire hash
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
        public async Task<Dictionary<string, string>> HashGetAsync(string key)
        {
            var hashvals = await database.HashGetAllAsync(key);
            return ConvertHashEntries(hashvals);
        }
        
		/// <summary>
		/// Serializes an object and stores it under a hash
		/// </summary>
		/// <param name="key"></param>
		/// <param name="obj"></param>
		/// <returns></returns>
        public async Task ObjectSetAsync(string key, object obj)
        {
            var dict = RedisConvert.Serialize(obj);
            if (dict.Keys.Count > 0)
            {
                await this.HashSetAsync(key, dict);
            }
            else
            {
                //TODO: Delete the key maybe?
                Console.WriteLine("Attempted to write a null key!");
            }
        }

		/// <summary>
		/// Gets a hash and deserializes into a object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
        public async Task<T> ObjectGetAsync<T>(string key)
        {
            //Get the hashset
            var dict = await this.HashGetAsync(key);
            return RedisConvert.Deserialize<T>(dict);
        }

		/// <summary>
		/// Adds a value to a set
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		/// <returns></returns>
        public async Task<long> SetAddAsync(string key, string value)
        {
            return await database.SetAddAsync(key, value) ? 1 : 0;            
        }

		/// <summary>
		/// Adds values to a set
		/// </summary>
		/// <param name="key"></param>
		/// <param name="values"></param>
		/// <returns></returns>
        public async Task<long> SetAddAsync(string key, params string[] values)
        {
            RedisValue[] redisValues = new RedisValue[values.Length];
            for (int i = 0; i < values.Length; i++) redisValues[i] = values[i];
            return await database.SetAddAsync(key, redisValues);
        }

		/// <summary>
		/// Gets all values in a set
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
        public async Task<string[]> SetGetAllAsync(string key)
        {
            RedisValue[] redisValues = await database.SetMembersAsync(key);
            string[] stringValues = new string[redisValues.Length];
            for (int i = 0; i < redisValues.Length; i++) stringValues[i] = redisValues[i];
            return stringValues;
        }

		/// <summary>
		/// Gets a random value in a set
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
        public async Task<string> SetGetRandomAsync(string key)
        {
            return await database.SetRandomMemberAsync(key);
        }
        
		/// <summary>
		/// Disposes the redis client
		/// </summary>
        public void Dispose()
        {
            redis.Dispose();
        }

        private HashEntry[] ConvertDictionary(Dictionary<string, string> dictionary)
        {
            int index = 0;
            HashEntry[] entries = new HashEntry[dictionary.Count];
            foreach (var kp in dictionary)
            {
                entries[index++] = new HashEntry(kp.Key, kp.Value);
            }

            return entries;
        }
        private Dictionary<string, string> ConvertHashEntries(HashEntry[] entries)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>(entries.Length);
            foreach (var entry in entries) dict.Add(entry.Name, entry.Value);
            return dict;
        }
    }
}