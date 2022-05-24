using System;
using System.Collections.Generic;
using System.Linq;

namespace StoicGoose.Common.Utilities
{
	public abstract class ConfigurationBase<T> where T : class, new()
	{
		public static readonly Dictionary<string, object> Defaults = default;

		static ConfigurationBase()
		{
			Defaults = GetDefaultValues();
		}

		private static Dictionary<string, object> GetDefaultValues()
		{
			var dict = new Dictionary<string, object>();
			var instance = new T();

			foreach (var property in typeof(T).GetProperties().Where(x => x.CanWrite))
			{
				var value = property.GetValue(instance);
				if (value == null || (property.PropertyType == typeof(string) && string.IsNullOrEmpty(value as string))) continue;
				dict.Add(property.Name, value);
			}

			return dict;
		}

		public void ResetToDefault(string name)
		{
			var property = GetType().GetProperty(name);
			if (property == null) throw new ArgumentException($"Setting '{name}' not found in {GetType().Name}", nameof(name));
			property.SetValue(this, Defaults[name]);
		}
	}

	public static class ConfigurationBase
	{
		public static void CopyConfiguration(object source, object destination)
		{
			if (source == null) throw new ArgumentNullException(nameof(source), "Source cannot be null");
			if (destination == null) throw new ArgumentNullException(nameof(destination), "Destination cannot be null");

			var sourceType = source.GetType();
			var destType = destination.GetType();

			foreach (var sourceProperty in sourceType.GetProperties().Where(x => x.CanRead))
			{
				var destProperty = destType.GetProperty(sourceProperty.Name);
				if (destProperty == null || !destProperty.CanWrite || destProperty.GetSetMethod(true) == null || destProperty.GetSetMethod(true).IsPrivate ||
					destProperty.GetSetMethod(true).Attributes.HasFlag(System.Reflection.MethodAttributes.Static) ||
					!destProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
					continue;

				var sourceValue = sourceProperty.GetValue(source, null);
				var destValue = destProperty.GetValue(destination, null);

				if ((sourceProperty.PropertyType.BaseType.IsGenericType ? sourceProperty.PropertyType.BaseType.GetGenericTypeDefinition() : sourceProperty.PropertyType.BaseType) == typeof(ConfigurationBase<>))
					CopyConfiguration(sourceValue, destValue);
				else
					destProperty.SetValue(destination, sourceValue);
			}
		}
	}
}
