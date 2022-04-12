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

		public override Vector2i StatusIconsLocation => new(0, DisplayControllerCommon.ScreenHeight);
		public override int StatusIconSize => 12;
		public override bool StatusIconsInverted => false;

		readonly static Dictionary<string, (string, Vector2i)> statusIcons = new()
		{
			{ "Power", ("Power.png", new(0, 0)) },
			{ "Initialized", ("Initialized.png", new(18, 0)) },
			{ "Sleep", ("Sleep.png", new(50, 0)) },
			{ "LowBattery", ("LowBattery.png", new(80, 0)) },
			{ "Volume0", ("Volume0.png", new(105, 0)) },
			{ "Volume1", ("Volume1.png", new(105, 0)) },
			{ "Volume2", ("Volume2.png", new(105, 0)) },
			{ "Headphones", ("Headphones.png", new(130, 0)) },
			{ "Horizontal", ("Horizontal.png", new(155, 0)) },
			{ "Vertical", ("Vertical.png", new(168, 0)) },
			{ "Aux1", ("Aux1.png", new(185, 0)) },
			{ "Aux2", ("Aux2.png", new(195, 0)) },
			{ "Aux3", ("Aux3.png", new(205, 0)) }
		};

		public override Dictionary<string, (string filename, Vector2i location)> StatusIcons => statusIcons;
	}

	public class WonderSwanColorMetadata : MetadataBase
	{
		public override string Manufacturer => "Bandai";
		public override string Model => "WonderSwan Color";

		public override Vector2i ScreenSize => new(DisplayControllerCommon.ScreenWidth, DisplayControllerCommon.ScreenHeight);
		public override double RefreshRate => DisplayControllerCommon.VerticalClock;

		public override string GameControls => "Start, B, A, X1, X2, X3, X4, Y1, Y2, Y3, Y4";
		public override string VerticalControlRemap => "Y2=X1, Y3=X2, Y4=X3, Y1=X4, X2=Y1, X3=Y2, X4=Y3, X1=Y4";
		public override string HardwareControls => "Volume";

		public override string InternalEepromFilename => "WonderSwanColor.eep";

		public override string RomFileFilter => "WonderSwan Color ROMs (*.wsc;*.ws)|*.wsc;*.ws";

		public override Vector2i StatusIconsLocation => new(DisplayControllerCommon.ScreenWidth, 0);
		public override int StatusIconSize => 12;
		public override bool StatusIconsInverted => true;

		readonly static Dictionary<string, (string, Vector2i)> statusIcons = new()
		{
			{ "Power", ("Power.png", new(0, 132)) },
			{ "Initialized", ("Initialized.png", new(0, 120)) },
			{ "Sleep", ("Sleep.png", new(0, 100)) },
			{ "LowBattery", ("LowBattery.png", new(0, 81)) },
			{ "Volume0", ("Volume0.png", new(0, 65)) },
			{ "Volume1", ("Volume1.png", new(0, 65)) },
			{ "Volume2", ("Volume2.png", new(0, 65)) },
			{ "Headphones", ("Headphones.png", new(0, 49)) },
			{ "Horizontal", ("Horizontal.png", new(0, 32)) },
			{ "Vertical", ("Vertical.png", new(0, 24)) },
			{ "Aux1", ("Aux1.png", new(0, 14)) },
			{ "Aux2", ("Aux2.png", new(0, 7)) },
			{ "Aux3", ("Aux3.png", new(0, 0)) }
		};

		public override Dictionary<string, (string filename, Vector2i location)> StatusIcons => statusIcons;
	}
}
