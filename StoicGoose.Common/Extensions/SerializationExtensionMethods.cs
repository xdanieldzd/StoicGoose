using System.IO;

using Newtonsoft.Json;

namespace StoicGoose.Common.Extensions
{
	public static class SerializationExtensionMethods
	{
		public static void SerializeToFile(this object obj, string jsonFileName)
		{
			SerializeToFile(obj, jsonFileName, new JsonSerializerSettings() { Formatting = Formatting.Indented });
		}

		public static void SerializeToFile(this object obj, string jsonFileName, JsonSerializerSettings serializerSettings)
		{
			using var writer = new StreamWriter(jsonFileName);
			writer.Write(JsonConvert.SerializeObject(obj, serializerSettings));
		}

		public static T DeserializeFromFile<T>(this string jsonFileName)
		{
			using var reader = new StreamReader(jsonFileName);
			return (T)JsonConvert.DeserializeObject(reader.ReadToEnd(), typeof(T), new JsonSerializerSettings() { Formatting = Formatting.Indented });
		}

		public static T DeserializeObject<T>(this string jsonString)
		{
			return (T)JsonConvert.DeserializeObject(jsonString, typeof(T), new JsonSerializerSettings() { Formatting = Formatting.Indented });
		}
	}
}
