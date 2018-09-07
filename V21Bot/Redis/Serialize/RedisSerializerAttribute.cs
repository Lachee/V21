using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace V21Bot.Redis.Serialize
{
	class RedisSerializerAttribute : Attribute
	{
		public Type Serializer { get; set; }		
		public RedisSerializerAttribute(Type t)
		{
			this.Serializer = t;
		}
	}
}
