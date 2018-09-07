using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Redis.Serialize
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class RedisPropertyAttribute : Attribute
	{
		public string DisplayName { get; set; }

		public RedisPropertyAttribute() : this(null) { }
		public RedisPropertyAttribute(string name)
		{
			this.DisplayName = name;
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class RedisIgnoreAttribute : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class RedisOptionAttribute : Attribute
	{
		public bool SerializeAll { get; set; }
		public RedisOptionAttribute() { SerializeAll = true; }
		public RedisOptionAttribute(bool serializeAll) { SerializeAll = serializeAll; }
	}
}
