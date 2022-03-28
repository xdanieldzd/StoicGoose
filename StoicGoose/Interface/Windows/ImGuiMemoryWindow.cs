using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ImGuiNET;

using StoicGoose.Emulation.Machines;

using NumericsVector2 = System.Numerics.Vector2;

namespace StoicGoose.Interface.Windows
{
	public class ImGuiMemoryWindow : ImGuiWindowBase
	{
		ImFontPtr japaneseFont = default;

		public ImGuiMemoryWindow() : base("Memory Editor", new NumericsVector2(600f, 400f), ImGuiCond.FirstUseEver)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		protected override void DrawWindow(params object[] args)
		{
			if (args.Length != 1 || args[0] is not IMachine machine) return;

			if (japaneseFont.Equals(default(ImFontPtr)))
				japaneseFont = ImGui.GetIO().Fonts.Fonts[1];

			if (ImGui.Begin(WindowTitle, ref isWindowOpen))
			{
				// TODO: actually write this thing and stuff



				var testData = new byte[] { 0x83, 0x65, 0x83, 0x58, 0x83, 0x67, 0x81, 0x49 };

				var dataByte = testData[0];
				var dataUshort = (ushort)(testData[0] << 0 | testData[1] << 8);
				var dataUint = (uint)(testData[0] << 0 | testData[1] << 8 | testData[2] << 16 | testData[3] << 24);
				var dataSjis = Encoding.GetEncoding(932).GetString(testData);

				ImGui.BeginGroup();
				{
					ImGui.Columns(4, "##display-column1", false);

					ImGui.TextUnformatted("8-bit:");
					ImGui.TextUnformatted("16-bit:");
					ImGui.TextUnformatted("32-bit:");
					ImGui.NextColumn();

					ImGui.TextUnformatted($"0x{dataByte:X2}");
					ImGui.TextUnformatted($"0x{dataUshort:X4}");
					ImGui.TextUnformatted($"0x{dataUint:X8}");
					ImGui.NextColumn();

					ImGui.TextUnformatted($"{dataByte}");
					ImGui.TextUnformatted($"{dataUshort}");
					ImGui.TextUnformatted($"{dataUint}");
					ImGui.NextColumn();

					ImGui.TextUnformatted($"{(sbyte)dataByte}");
					ImGui.TextUnformatted($"{(short)dataUshort}");
					ImGui.TextUnformatted($"{(int)dataUint}");
					ImGui.NextColumn();

					ImGui.EndGroup();
				}

				ImGui.Columns();

				ImGui.TextUnformatted("Shift-JIS:"); ImGui.SameLine();
				ImGui.PushFont(japaneseFont);
				ImGui.TextUnformatted(dataSjis);

				// https://ja.wikipedia.org/w/index.php?title=%E3%82%86%E3%82%8B%E3%82%AD%E3%83%A3%E3%83%B3%E2%96%B3&oldid=88753788#%E4%BD%9C%E9%A2%A8
				//ImGui.TextWrapped("漫画の流れとしては、女子高校生たちが個人またはグループでのキャンプを計画するところからエピソードが始まり、道具や食材を準備して目的地まで旅した後、見晴らしのよい現地からの展望を満喫しながら、用意していた食材を現地で野外調理する、あるいは食堂でご当地グルメに舌鼓を打つ、温泉を満喫するなどを経て、テントで1泊して翌朝を迎える、という展開の繰り返しで進行する。本作では一人での気ままなキャンプと大人数での賑やかなキャンプの魅力が対等のものとして描かれており、主要登場人物同士はいつでも行動を共にしているわけではなく一人旅のエピソードも多いが、旅行先からSNSで互いの近況を報告し合う形でストーリーに関わっていく。　作者のあfろは、元々ツーリングを趣味としていたことが本作の着想に繋がったといい、作者自身のアウトドア経験が作品に盛り込まれている。執筆に当たっては実在のキャンプ場や観光地の下見、本番の取材、後から気になった箇所の再取材と、1箇所につき2回から3回の取材を行っているといい、漫画には取材先の風景がそのまま描かれている。見開きのページで精緻に描かれるキャンプ場からの展望や、それを眺める登場人物の表情などが、臨場感や、舞台となる実在の観光名所に対する興味を煽る見せ場となっている。アウトドア趣味に向き合う少女たちのまっすぐな魅力や、日常の合間に披露される、火起こしのノウハウや寝具の種類といったアウトドア知識の描写、うまそうに描かれる食べ物、実在するキャンプ道具の描写、旅先で出会うさまざまな犬にまつわる話題なども、作品の持ち味になっている。　漫画のタイトルロゴ末尾にある三角記号「△」は、キャンプ・テントのピクトグラムになっており、表題を音読する場合は発音されない。");
				ImGui.PopFont();

				ImGui.End();
			}
		}
	}
}
