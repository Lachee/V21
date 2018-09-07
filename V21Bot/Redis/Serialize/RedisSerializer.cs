using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Redis.Serialize
{
	public abstract class RedisSerializer
	{
		public RedisSerializer() { }
		public abstract object Deserialize(Dictionary<string, string> dictionary, SerializeMember member, string key);
		public abstract void Serialize(Dictionary<string, string> buffer, SerializeMember member, object reference, string key);
	}
}
