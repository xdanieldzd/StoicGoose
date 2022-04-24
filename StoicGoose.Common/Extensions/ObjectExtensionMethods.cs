using Newtonsoft.Json;

namespace StoicGoose.Common.Extensions
{
	public static class ObjectExtensionMethods
	{
		/* https://dotnetcoretutorials.com/2020/09/09/cloning-objects-in-c-and-net-core/ */
		public static T Clone<T>(this T source)
		{
			if (source is null) return default;
			return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, new JsonSerializerSettings()
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			}), new JsonSerializerSettings()
			{
				ObjectCreationHandling = ObjectCreationHandling.Replace
			});
		}
	}
}
