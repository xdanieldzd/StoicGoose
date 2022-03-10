using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace StoicGoose.Extensions
{
	public static class DictionaryExtensionMethods
	{
		/* Based on https://stackoverflow.com/a/30300521 */
		public static Dictionary<string, T> KeyMatchesWildcard<T>(this Dictionary<string, T> dictionary, string wildcard)
		{
			return dictionary.Where(x => Regex.IsMatch(x.Key, "^" + Regex.Escape(wildcard).Replace("\\*", ".*") + "$")).ToDictionary(x => x.Key, y => y.Value);
		}
	}
}
