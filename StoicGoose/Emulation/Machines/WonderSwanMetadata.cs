using System.Collections.Generic;

using OpenTK.Mathematics;

using StoicGoose.Emulation.Display;

namespace StoicGoose.Emulation.Machines
{
	public class WonderSwanMetadata : MetadataBase
	{
		public override string Manufacturer => "Bandai";
		public override string Model => "WonderSwan";

		public override Vector2i ScreenSize => new(DisplayControllerCommon.ScreenWidth, DisplayControllerCommon.ScreenHeight);
		public override double RefreshRate => DisplayControllerCommon.VerticalClock;

		public override string GameControls => "Start, B, A, X1, X2, X3, X4, Y1, Y2, Y3, Y4";
		public override string VerticalControlRemap => "Y2=X1, Y3=X2, Y4=X3, Y1=X4, X2=Y1, X3=Y2, X4=Y3, X1=Y4";
		public override string HardwareControls => "Volume";

		public override string InternalEepromFilename => "WonderSwan.eep";

		public override string RomFileFilter => "WonderSwan ROMs (*.ws)|*.ws";

		public override int StatusIconSize => 12;

		readonly static Dictionary<string, (string, Vector2i)> statusIcons = new()
		{
			{ "Power", ("Power.png", new(0, 144)) },
			{ "Initialized", ("Initialized.png", new(18, 144)) },
			{ "Sleep", ("Sleep.png", new(50, 144)) },
			{ "LowBattery", ("LowBattery.png", new(80, 144)) },
			{ "Volume0", ("Volume0.png", new(105, 144)) },
			{ "Volume1", ("Volume1.png", new(105, 144)) },
			{ "Volume2", ("Volume2.png", new(105, 144)) },
			{ "Headphones", ("Headphones.png", new(130, 144)) },
			{ "Horizontal", ("Horizontal.png", new(155, 144)) },
			{ "Vertical", ("Vertical.png", new(168, 144)) },
			{ "Aux1", ("Aux1.png", new(185, 144)) },
			{ "Aux2", ("Aux2.png", new(195, 144)) },
			{ "Aux3", ("Aux3.png", new(205, 144)) },
		};

		public override Dictionary<string, (string filename, Vector2i location)> StatusIcons => statusIcons;
	}
}
