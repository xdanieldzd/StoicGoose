using System.Collections.Generic;

using OpenTK.Mathematics;

using StoicGoose.Emulation.Display;

namespace StoicGoose.Emulation.Machines
{
	public class WonderSwanColorMetadata : MetadataBase
	{
		public override string Manufacturer => "Bandai";
		public override string Model => "WonderSwan Color";

		public override Vector2i ScreenSize => new(DisplayControllerCommon.ScreenWidth, DisplayControllerCommon.ScreenHeight);
		public override double RefreshRate => DisplayControllerCommon.VerticalClock;

		public override string GameControls => "start, b, a, x1, x2, x3, x4, y1, y2, y3, y4";
		public override string HardwareControls => "volume";

		public override string InternalEepromFilename => "WonderSwanColor.eep";

		public override string RomFileFilter => "WonderSwan Color ROMs (*.wsc;*.ws)|*.wsc;*.ws";

		public override int StatusIconSize => 12;

		readonly static Dictionary<string, (string, Vector2i)> statusIcons = new()
		{
			{ "power", ("Power.png", new(0, 144)) },
			{ "initialized", ("Initialized.png", new(18, 144)) },
			{ "sleep", ("Sleep.png", new(50, 144)) },
			{ "lowbatt", ("LowBattery.png", new(80, 144)) },
			{ "volume0", ("Volume0.png", new(105, 144)) },
			{ "volume1", ("Volume1.png", new(105, 144)) },
			{ "volume2", ("Volume2.png", new(105, 144)) },
			{ "headphones", ("Headphones.png", new(130, 144)) },
			{ "horizontal", ("Horizontal.png", new(155, 144)) },
			{ "vertical", ("Vertical.png", new(168, 144)) },
			{ "aux1", ("Aux1.png", new(185, 144)) },
			{ "aux2", ("Aux2.png", new(195, 144)) },
			{ "aux3", ("Aux3.png", new(205, 144)) },
		};

		public override Dictionary<string, (string filename, Vector2i location)> StatusIcons => statusIcons;
	}
}
