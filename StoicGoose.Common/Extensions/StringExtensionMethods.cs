using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace StoicGoose.Common.Extensions
{
	public static class StringExtensionMethods
	{
		/* Modified from https://stackoverflow.com/a/2641383 */
		public static List<int> IndexOfAll(this string str, string value)
		{
			if (string.IsNullOrEmpty(value))
				throw new ArgumentException("Search string is null or empty", nameof(value));

			var idxs = new List<int>();
			for (var i = 0; ; i += value.Length)
			{
				i = str.IndexOf(value, i);
				if (i == -1) return idxs;
				idxs.Add(i);
			}
		}

		public static string EnsureEndsWithPeriod(this string str) => str + (!str.EndsWith('.') ? "." : string.Empty);

		/* Regex via https://superuser.com/a/380778 */
		public static string RemoveAnsi(this string str) => Regex.Replace(str, @"\x1b\[[0-9;]*[mGKHF]", "");
	}
}
