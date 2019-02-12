using V21Bot.Redis.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Redis
{
	public static class RedisNamespace
	{
		public static string RootNamespace { get; set; } = "";
		public static string NamespaceSeperator { get; set; } = ":";

        public static void SetRoot(params object[] folders) { RootNamespace = Create(folders); }
		public static string Create(params object[] folders)
		{
			StringBuilder builder = new StringBuilder(RootNamespace);
			if (folders.Length > 0)
			{
				if (!string.IsNullOrEmpty(RootNamespace))
					builder.Append(NamespaceSeperator);

				builder.Append(folders[0]);
				for (int i = 1; i < folders.Length; i++)
					builder.Append(NamespaceSeperator).Append(folders[i]);
			}
			return builder.ToString();
		}
	}

	public interface IRedisClient : IDisposable
	{
		Task Initialize();

		Task StoreStringAsync(string key, string value, TimeSpan? TTL = null);
		Task<string> FetchStringAsync(string key, string @default = null);

		Task StoreHashMapAsync(string key, Dictionary<string, string> values);
        Task<Dictionary<string, string>> FetchHashMapAsync(string key);		
        Task StoreObjectAsync(string key, object obj);
		Task<T> FetchObjectAsync<T>(string key);
		
		Task<long> AddHashSetAsync(string key, string value);
        Task<long> AddHashSetAsync(string key, params string[] values);
        Task<long> AddHashSetAsync(string key, HashSet<string> values);
        Task<bool> RemoveHashSetASync(string key, string value);

        Task<string> FetchRandomHashSetElementAsync(string key);
        Task<HashSet<string>> FetchHashSetAsync(string key);

        Task SetExpiryAsync(string key, TimeSpan ttl);
        Task<bool> RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}
