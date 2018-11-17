using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

namespace V21Bot.Redis.Serialize
{
	class RedisConvert
	{
		/// <summary>
		/// Deserializes a dictionary into a object
		/// </summary>
		/// <typeparam name="T">The type</typeparam>
		/// <param name="buffer">The data that was received by the redis</param>
		/// <returns></returns>
		public static T Deserialize<T>(Dictionary<string, string> buffer)
		{
            //Buffer is null, return default.
            if (buffer == null)
                return default(T);

			//Prepare the type
			Type type = typeof(T);
			var constructor = type.GetConstructor(new Type[0]);
			object reference = constructor.Invoke(new object[0]);

			//Deserialize, returning default if we find jack all
			if (!Deserialize(buffer, type, ref reference))
				return default(T);

			//Return a cast
			return (T) reference;
		}

		/// <summary>
		/// Deserializes a dictionary into a object
		/// </summary>
		/// <param name="buffer">The data that was received by the redis</param>
		/// <param name="type">The type of target object</param>
		/// <param name="reference">The target object</param>
		/// <param name="subkey">The subkey (optional)</param>
		/// <returns></returns>
		private static bool Deserialize(Dictionary<string, string> buffer, Type type, ref object reference, string subkey = "")
		{
			//Prepare the type and initial options
			bool hasElements = false;
			bool serializeAll = false;

			//See if we should override the options
			RedisOptionAttribute options = type.GetCustomAttribute<RedisOptionAttribute>();
			if (options != null)
			{
				serializeAll = options.SerializeAll;
			}

			//Create a new instance of the type
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				if (DeserializeMember(buffer, type, new SerializeMember(property), ref reference, subkey, serializeAll))
					hasElements = true;

			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				if (DeserializeMember(buffer, type, new SerializeMember(field), ref reference, subkey, serializeAll))
					hasElements = true;

			//Return the element state
			return hasElements;
		}

		private static bool DeserializeMember(Dictionary<string, string> buffer, Type type, SerializeMember member, ref object reference, string subkey, bool serializeAll)
		{
			var ignoreAttribute = member.GetCustomAttribute<RedisIgnoreAttribute>();
			if (ignoreAttribute != null) return false;

			//Ignore generated blocks
			var generatedAttribute = member.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();
			if (generatedAttribute != null) return false;

			var attribute = member.GetCustomAttribute<RedisPropertyAttribute>();
			if (serializeAll || attribute != null)
			{
				//Prepare its key
				string key = PrepareKey(attribute, member.Name, subkey);

				//If class, we have to do something completely different
				//If it has a serialize attribute, we want to construct its serializer
				var serializerAttribute = member.GetCustomAttribute<RedisSerializerAttribute>();
				if (serializerAttribute != null)
				{
					//They have a custom serializer, so lets construct its type
					var constructor = serializerAttribute.Serializer.GetConstructor(new Type[0]);
					if (constructor != null)
					{
						var serializer = constructor.Invoke(new object[0]) as RedisSerializer;
						if (serializer != null)
						{
							object v = serializer.Deserialize(buffer, member, key);
							member.SetValue(reference, v);
							return true;
						}

						throw new Exception("Bad Serialization on the custom serializer! Failed to cast into a RedisSerializer");
					}

					throw new Exception("Bad Serialization on the custom serializer! Failed to find a constructor with 0 elements");
				}

				//If the property is a string, just cast ez pz
				if (member.IsPrimitive || member.IsString || member.IsEnum)
				{
					string primval;
					if (buffer.TryGetValue(key, out primval))
					{
						if (member.IsPrimitive || member.IsEnum)
						{
							object v = TypeDescriptor.GetConverter(member.Type).ConvertFromString(primval);
							member.SetValue(reference, v);
						}
						else
						{
							member.SetValue(reference, primval);
						}

						return true;
					}

					return false;
				}

				//We have to do it the classical way with a subkey
				//var propvalConstructor = propertyType.GetConstructor(new Type[0]);
				//object propval = propvalConstructor.Invoke(new object[0]);
				object propval = null;
				try
				{
					propval = Activator.CreateInstance(member.Type);
				}
				catch (Exception e)
				{
					Console.WriteLine("Exception while creating a instance!");
					throw e;
				}

				//Serialize
				if (propval != null && Deserialize(buffer, member.Type, ref propval, key + "."))
				{
					member.SetValue(reference, propval);
					return true;
				}
			}

			return false;
		}

		#region Serialize

		/// <summary>
		/// Serializes an object into a single Dictionary.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static Dictionary<string, string> Serialize(object obj)
		{
			Dictionary<string, string> buffer = new Dictionary<string, string>();
			Serialize(buffer, obj);
			return buffer;
		}

		/// <summary>
		/// Serializes a object into a single dictionary
		/// </summary>
		/// <param name="buffer">The dictionary that all the keys will be inserted into</param>
		/// <param name="obj">The object to parse</param>
		/// <param name="subkey">The current subkey</param>
		public static void Serialize(Dictionary<string, string> buffer, object obj, string subkey = "")
		{

			//Prepare the type and initial options
			Type type = obj.GetType();
			bool serializeAll = false;

			//See if we should override the options
			RedisOptionAttribute options = type.GetCustomAttribute<RedisOptionAttribute>();
			if (options != null)
			{
				serializeAll = options.SerializeAll;
			}

			//Iterate over every property
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				SerializeMember(buffer, new SerializeMember(property), obj, subkey, serializeAll);

			//Iterate over every field
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				SerializeMember(buffer, new SerializeMember(field), obj, subkey, serializeAll);
		}		
		private static void SerializeMember(Dictionary<string, string> buffer, SerializeMember member, object reference, string subkey, bool serializeAll)
		{

			var ignoreAttribute = member.GetCustomAttribute<RedisIgnoreAttribute>();
			if (ignoreAttribute != null) return;
			if (serializeAll)
			{
				Console.WriteLine("WARNING: SERIALIZE ALL ENABLED");
			}

			//Ignore generated blocks
			var generatedAttribute = member.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>();
			if (generatedAttribute != null) return;


			var attribute = member.GetCustomAttribute<RedisPropertyAttribute>();
			if (serializeAll || attribute != null)
			{
				//Prepare its key
				string key = PrepareKey(attribute, member.Name, subkey);

				//If it has a serialize attribute, we want to construct its serializer
				var serializerAttribute = member.GetCustomAttribute<RedisSerializerAttribute>();
				if (serializerAttribute != null)
				{
					//They have a custom serializer, so lets construct its type
					var constructor = serializerAttribute.Serializer.GetConstructor(new Type[0]);
					if (constructor != null)
					{
						var serializer = constructor.Invoke(new object[0]) as RedisSerializer;
						if (serializer != null)
						{
							serializer.Serialize(buffer, member, reference, key);
							return;
						}

						throw new Exception("Bad Serialization on the custom serializer! Failed to cast into a RedisSerializer");
					}

					throw new Exception("Bad Serialization on the custom serializer! Failed to find a constructor with 0 elements");
				}

				//Make sure its a object
				if (member.IsPrimitive || member.IsEnum || member.IsString)
				{
					//Add it to the dictionary
					object v = member.GetValue(reference);
					if (v != null) buffer.Add(key, v.ToString());
					return;
				}

				//Everything else fails, so do classical serialization
				object propval = member.GetValue(reference);
				if (propval != null) Serialize(buffer, propval, key + ".");
			}		
		}

		#endregion
		
		private static T Construct<T>()
		{
			var t = typeof(T);
			var constructor = t.GetConstructor(new Type[0]);
			return (T)constructor.Invoke(new object[0]);
		}
		
		/// <summary>
		/// Creates a hashmap key name
		/// </summary>
		/// <param name="attr"></param>
		/// <param name="prop"></param>
		/// <param name="subkey"></param>
		/// <returns></returns>
		private static string PrepareKey(RedisPropertyAttribute attr, string propName, string subkey)
		{
			return subkey + (attr != null && attr.DisplayName != null ? attr.DisplayName : propName);
		}
	}

	/// <summary>
	/// Serialized Member
	/// </summary>
	public class SerializeMember
	{
		public PropertyInfo Property { get; set; }
		public FieldInfo Field { get; set; }
		public string Name { get; set; }
		public Type Type { get; set; }
		public bool IsProperty => Property != null;
		public bool IsField => Field != null;

		public bool IsPrimitive => Type.IsPrimitive;
		public bool IsEnum => Type.IsEnum;
		public bool IsString => Type == typeof(string);

		public SerializeMember(PropertyInfo property)
		{
			Property = property;
			Field = null;
			Name = property.Name;
			Type = property.PropertyType;
		}

		public SerializeMember(FieldInfo field)
		{
			Property = null;
			Field = field;
			Name = field.Name;
			Type = field.FieldType;
		}

		public object GetValue(object reference)
		{
			return IsProperty ? Property.GetValue(reference) : Field.GetValue(reference);
		}
		public void SetValue(object reference, object value)
		{
			if (IsProperty)
				Property.SetValue(reference, value);
			else
				Field.SetValue(reference, value);
		}

		public T GetCustomAttribute<T>() where T : Attribute
		{
			if (IsProperty)
				return Property.GetCustomAttribute<T>();
			return Field.GetCustomAttribute<T>();
		}

		public MemberInfo GetMember()
		{
			return IsProperty ? (MemberInfo)Property : (MemberInfo)Field;
		}
		public static implicit operator MemberInfo(SerializeMember member)
		{
			return member.GetMember();
		}
	}
}
