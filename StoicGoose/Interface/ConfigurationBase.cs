using System;
using System.Collections.Generic;
using System.Linq;

namespace StoicGoose.Interface
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
}
