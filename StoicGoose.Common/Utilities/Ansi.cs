using System;
using System.Collections.Generic;
using System.Text;

namespace StoicGoose.Common.Utilities
{
	public static class Ansi
	{
		public readonly static string Reset = "\x1B[0m";
		public readonly static string Black = "\x1B[30m";
		public readonly static string Red = "\x1B[31m";
		public readonly static string Green = "\x1B[32m";
		public readonly static string Yellow = "\x1B[33m";
		public readonly static string Blue = "\x1B[34m";
		public readonly static string Magenta = "\x1B[35m";
		public readonly static string Cyan = "\x1B[36m";
		public readonly static string White = "\x1B[37m";

		public static string RGB(byte r, byte g, byte b) => $"\x1B[38;2;{r};{g};{b}m";

		// Such a stupid gimmick... but hey, I like stupid gimmicks and I especially like making them, so whatever~
		public static string Gradient(string text, bool useHsl, params (byte r, byte g, byte b)[] colors)
		{
			var stepsPerColor = (int)Math.Round(text.Length / (colors.Length - 1f), MidpointRounding.AwayFromZero);
			var steps = Math.Max(stepsPerColor * (colors.Length - 1), text.Length);

			List<(byte r, byte g, byte b)> gradient = new();

			for (int i = 0, c = 0; i < steps; i += stepsPerColor, c++)
			{
				// TODO: this is a workaround for a out-of-range bug, but ugh, it's for a mere gimmick barely anyone will ever see, soooooo... whatever!
				if (c + 1 >= colors.Length) c--;

				if (useHsl)
				{
					var (h1, s1, l1) = RgbToHsl(colors[c + 0].r, colors[c + 0].g, colors[c + 0].b);
					var (h2, s2, l2) = RgbToHsl(colors[c + 1].r, colors[c + 1].g, colors[c + 1].b);

					for (var j = 0; j < stepsPerColor; j++)
					{
						var by = Math.Clamp(j / 1f / ((stepsPerColor - 1) / 1f), 0f, 1f);
						var (h, s, l) = Lerp(h1, s1, l1, h2, s2, l2, by);
						gradient.Add(HslToRgb(h, s, l));
					}
				}
				else
				{
					var (r1, g1, b1) = (colors[c + 0].r / 255f, colors[c + 0].g / 255f, colors[c + 0].b / 255f);
					var (r2, g2, b2) = (colors[c + 1].r / 255f, colors[c + 1].g / 255f, colors[c + 1].b / 255f);

					for (var j = 0; j < stepsPerColor; j++)
					{
						var by = Math.Clamp(j / 1f / ((stepsPerColor - 1) / 1f), 0f, 1f);
						gradient.Add(((byte)(Lerp(r1, r2, by) * 255), (byte)(Lerp(g1, g2, by) * 255), (byte)(Lerp(b1, b2, by) * 255)));
					}
				}
			}

			var builder = new StringBuilder();
			for (var i = 0; i < Math.Min(gradient.Count, text.Length); i++)
				builder.Append($"{RGB(gradient[i].r, gradient[i].g, gradient[i].b)}{text[i]}");
			return builder.ToString();
		}

		private static float Lerp(float v1, float v2, float by) => v1 * (1f - by) + v2 * by;
		private static (float h, float s, float l) Lerp(float h1, float s1, float l1, float h2, float s2, float l2, float by) => (Lerp(h1, h2, by) % 360f, Math.Clamp(Lerp(s1, s2, by), 0f, 1f), Math.Clamp(Lerp(l1, l2, by), 0f, 1f));

		// http://www.easyrgb.com/en/math.php
		private static (float h, float s, float l) RgbToHsl(byte red, byte green, byte blue)
		{
			float h = 0f, s, l;

			var r = red / 255f;
			var g = green / 255f;
			var b = blue / 255f;

			var min = Math.Min(Math.Min(r, g), b);
			var max = Math.Max(Math.Max(r, g), b);
			var deltaMax = max - min;

			l = (max + min) / 2f;

			if (deltaMax == 0)
			{
				h = 0;
				s = 0;
			}
			else
			{
				if (l < 0.5f) s = deltaMax / (max + min);
				else s = deltaMax / (2f - max - min);

				var deltaR = ((max - r) / 6f + deltaMax / 2f) / deltaMax;
				var deltaG = ((max - g) / 6f + deltaMax / 2f) / deltaMax;
				var deltaB = ((max - b) / 6f + deltaMax / 2f) / deltaMax;

				if (r == max) h = deltaB - deltaG;
				else if (g == max) h = 1f / 3f + deltaR - deltaB;
				else if (b == max) h = 2f / 3f + deltaG - deltaR;

				if (h < 0f) h++;
				if (h > 1f) h--;
			}

			return (h, s, l);
		}

		// http://www.easyrgb.com/en/math.php
		private static (byte r, byte g, byte b) HslToRgb(float hue, float saturation, float lightness)
		{
			byte r, g, b;

			if (saturation == 0f)
			{
				r = (byte)(lightness * 255);
				g = (byte)(lightness * 255);
				b = (byte)(lightness * 255);
			}
			else
			{
				float v1, v2;

				if (lightness < 0.5f) v2 = lightness * (1f + saturation);
				else v2 = lightness + saturation - saturation * lightness;

				v1 = 2f * lightness - v2;

				r = (byte)(255 * HueToRgb(v1, v2, hue + 1f / 3f));
				g = (byte)(255 * HueToRgb(v1, v2, hue));
				b = (byte)(255 * HueToRgb(v1, v2, hue - 1f / 3f));
			}

			return (r, g, b);
		}

		private static float HueToRgb(float v1, float v2, float vh)
		{
			if (vh < 0f) vh++;
			if (vh > 1) vh--;

			if (6f * vh < 1f) return v1 + (v2 - v1) * 6f * vh;
			if (2f * vh < 1f) return v2;
			if (3f * vh < 2f) return v1 + (v2 - v1) * (2f / 3f - vh) * 6f;
			return v1;
		}
	}
}
