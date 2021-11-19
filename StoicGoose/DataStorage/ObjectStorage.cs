using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StoicGoose.DataStorage
{
	public class ObjectStorage
	{
		static readonly Dictionary<Type, MethodInfo> conversionMethods = new();

		readonly Dictionary<string, object> storageDict = new();

		public ObjectValue Value { get; set; }

		static ObjectStorage()
		{
			foreach (var methodInfo in typeof(ObjectValue).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
				conversionMethods.Add(methodInfo.ReturnType, methodInfo);
		}

		public static implicit operator string(ObjectStorage storage) => storage.Value;

		public ObjectStorage this[params string[] path]
		{
			get
			{
				if (path[0].Contains("/")) path = path[0].Split('/');
				var key = path[0];
				if (!storageDict.ContainsKey(key)) storageDict.Add(key, new ObjectStorage());
				var remaining = path.Skip(1).ToArray();
				if (remaining.Length > 0) return (storageDict[key] as ObjectStorage)[remaining];
				else return storageDict[key] as ObjectStorage;
			}
			set
			{
				if (path[0].Contains("/")) path = path[0].Split('/');
				var key = path[0];
				if (!storageDict.ContainsKey(key)) storageDict.Add(key, new ObjectStorage());
				var remaining = path.Skip(1).ToArray();
				if (remaining.Length > 0) (storageDict[key] as ObjectStorage)[remaining] = value;
				else (storageDict[key] as ObjectStorage).Value = value.Value;
			}
		}

		public Dictionary<string, object> GetStorage() => storageDict;

		public T Get<T>() => (T)conversionMethods[typeof(T)].Invoke(this, new object[] { (string)Value });

		public override string ToString() => Value;
	}
}
