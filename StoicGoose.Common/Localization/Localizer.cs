using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StoicGoose.Common.Localization
{
	public static class Localizer
	{
		public static string FallbackCulture { get; set; } = "en";

		static JObject source = default;

		public static void Initialize(string jsonData) => source = JsonConvert.DeserializeObject(jsonData) as JObject;

		public static CultureInfo[] GetSupportedLanguages() => source?.Children().Select(x => new CultureInfo((x as JProperty).Name)).ToArray() ?? Array.Empty<CultureInfo>();

		private static JToken GetToken(string path) => source?.SelectToken($"{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.{path}") ?? source?.SelectToken($"{FallbackCulture}.{path}");
		public static string GetString(string path) => GetToken(path)?.Value<string>() ?? path[(path.LastIndexOf('.') + 1)..];
		public static string GetString(string path, object parameters)
		{
			var result = GetString(path);
			var properties = parameters.GetType().GetProperties();
			foreach (Match match in Regex.Matches(result, @"{(?<param>[^}:]*):*(?<format>[^}]*)}").Where(x => x.Success))
			{
				var property = properties.First(x => x.Name == match.Groups["param"].Value);
				var format = match.Groups["format"].Value;
				var formattedValue = string.IsNullOrEmpty(format) ? $"{property.GetValue(parameters)}" : string.Format($"{{0:{format}}}", property.GetValue(parameters));
				result = result.Replace(match.Value, formattedValue);
			}
			return result;
		}
	}
}
