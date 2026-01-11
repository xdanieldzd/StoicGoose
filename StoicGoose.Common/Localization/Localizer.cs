using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace StoicGoose.Common.Localization
{
    public static partial class Localizer
    {
        public static string FallbackCulture { get; set; } = "en";

        static JObject source = default;

        public static void Initialize(string jsonData) => source = JsonConvert.DeserializeObject(jsonData) as JObject;

        public static CultureInfo[] GetSupportedLanguages() => source?.Children().Select(x => new CultureInfo((x as JProperty).Name)).ToArray() ?? [];

        private static JToken GetToken(string path) => source?.SelectToken($"{CultureInfo.CurrentUICulture.TwoLetterISOLanguageName}.{path}") ?? source?.SelectToken($"{FallbackCulture}.{path}");
        public static string GetString(string path) => GetToken(path)?.Value<string>() ?? path[(path.LastIndexOf('.') + 1)..];
        public static string GetString(string path, object parameters)
        {
            var result = GetString(path);
            var properties = parameters.GetType().GetProperties();
            foreach (Match match in GetStringRegex().Matches(result).Where(x => x.Success))
            {
                var property = properties.First(x => x.Name == match.Groups["param"].Value);
                var format = match.Groups["format"].Value;
                var formattedValue = string.IsNullOrEmpty(format) ? $"{property.GetValue(parameters)}" : string.Format($"{{0:{format}}}", property.GetValue(parameters));
                result = result.Replace(match.Value, formattedValue);
            }
            return result;
        }

        [GeneratedRegex(@"{(?<param>[^}:]*):*(?<format>[^}]*)}")]
        private static partial Regex GetStringRegex();
    }
}
