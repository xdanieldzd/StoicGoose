using System;

using Newtonsoft.Json;

namespace StoicGoose.Extensions
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

		/* https://stackoverflow.com/a/1130781 */
		public static bool IsNumber(this object obj)
		{
			if (Equals(obj, null)) return false;

			var objType = Nullable.GetUnderlyingType(obj.GetType()) ?? obj.GetType();
			if (objType.IsPrimitive)
				return objType != typeof(bool) && objType != typeof(char) && objType != typeof(IntPtr) && objType != typeof(UIntPtr);

			return objType == typeof(decimal);
		}
	}
}
