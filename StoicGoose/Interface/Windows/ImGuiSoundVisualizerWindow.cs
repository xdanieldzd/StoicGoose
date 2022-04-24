using System.Linq;

using ImGuiNET;

using StoicGoose.Core.Sound;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiSoundVisualizerWindow : ImGuiWindowBase
	{
		const float offset = 0.025f;
		const float height = 50f;

		public ImGuiSoundVisualizerWindow() : base("Sound Visualizer", new NumericsVector2(600f, 680f), ImGuiCond.FirstUseEver) { }

		protected override void DrawWindow(object userData)
		{
			if (userData is not SoundControllerCommon soundController) return;

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				if (soundController.LastEnqueuedMixedSamples.Length != 0)
				{
					for (var i = 0; i < soundController.NumChannels; i++)
					{
						var channelSamples = soundController.LastEnqueuedChannelSamples[i].Select(x => (float)x / ushort.MaxValue).ToArray();
						var channelMax = channelSamples.Max();
						ImGui.PlotLines($"Channel {i + 1} (Left)", ref channelSamples[0], channelSamples.Length / 2, 0, string.Empty, -offset, channelMax + offset, new NumericsVector2(0f, height), 8);
						ImGui.PlotLines($"Channel {i + 1} (Right)", ref channelSamples[1], channelSamples.Length / 2, 0, string.Empty, -offset, channelMax + offset, new NumericsVector2(0f, height), 8);
					}

					var mixedSamples = soundController.LastEnqueuedMixedSamples.Select(x => (float)x / ushort.MaxValue).ToArray();
					var mixedMax = mixedSamples.Max();
					ImGui.PlotLines("Output (Left)", ref mixedSamples[0], mixedSamples.Length / 2, 0, string.Empty, -offset, mixedMax + offset, new NumericsVector2(0f, height), 8);
					ImGui.PlotLines("Output (Right)", ref mixedSamples[1], mixedSamples.Length / 2, 0, string.Empty, -offset, mixedMax + offset, new NumericsVector2(0f, height), 8);
				}

				ImGui.End();
			}
		}
	}
}
