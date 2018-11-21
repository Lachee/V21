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

		Task StringSetAsync(string key, string value, TimeSpan? TTL = null);
		Task<string> StringGetAsync(string key, string @default = null);

		Task HashSetAsync(string key, Dictionary<string, string> values);
        Task<Dictionary<string, string>> HashGetAsync(string key);
		
        Task ObjectSetAsync(string key, object obj);
		Task<T> ObjectGetAsync<T>(string key);
		 


		Task<long> SetAddAsync(string key, string value);
        Task<long> SetAddAsync(string key, params string[] values);
        Task<long> SetAddAsync(string key, HashSet<string> values);

        Task<bool> SetRemoveAsync(string key, string value);

        Task<string> SetGetRandomAsync(string key);
        Task<HashSet<string>> SetGetAsync(string key);

        Task ExpireAsync(string key, TimeSpan ttl);
        Task<bool> RemoveAsync(string key);
    }
}
