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
		public override string InternalEepromDefaultUsername => "WONDERSWAN";
		public override Dictionary<ushort, byte> InternalEepromDefaultData => new()
		{
			{ 0x70, 0x19 }, // Year of birth [just for fun, here set to original WS release date; new systems probably had no date set?]
			{ 0x71, 0x99 }, // ""
			{ 0x72, 0x03 }, // Month of birth [again, WS release for fun]
			{ 0x73, 0x04 }, // Day of birth [and again]
			{ 0x74, 0x00 }, // Sex [set to ?]
			{ 0x75, 0x00 }, // Blood type [set to ?]

			{ 0x76, 0x00 }, // Last game played, publisher ID [set to presumably none]
			{ 0x77, 0x00 }, // ""
			{ 0x78, 0x00 }, // Last game played, game ID [set to presumably none]
			{ 0x79, 0x00 }, // ""
			{ 0x7A, 0x00 }, // Swan ID (see Mama Mitte) -- TODO: set to valid/random value?
			{ 0x7B, 0x00 }, // ""
			{ 0x7C, 0x00 }, // Number of different games played [set to presumably none]
			{ 0x7D, 0x00 }, // Number of times settings were changed [set to presumably none]
			{ 0x7E, 0x00 }, // Number of times powered on [set to presumably none]
			{ 0x7F, 0x00 }  // ""
		};

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
			{ "Volume0", ("VolumeA0.png", new(105, 0)) },
			{ "Volume1", ("VolumeA1.png", new(105, 0)) },
			{ "Volume2", ("VolumeA2.png", new(105, 0)) },
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
		public override string InternalEepromDefaultUsername => "WONDERSWANCOLOR";
		public override Dictionary<ushort, byte> InternalEepromDefaultData => new()
		{
			{ 0x70, 0x20 }, // Year of birth [set to WSC release date for fun]
			{ 0x71, 0x00 }, // ""
			{ 0x72, 0x12 }, // Month of birth [again]
			{ 0x73, 0x09 }, // Day of birth [again]
			{ 0x74, 0x00 }, // Sex [?]
			{ 0x75, 0x00 }, // Blood type [?]

			{ 0x76, 0x00 }, // Last game played, publisher ID [none]
			{ 0x77, 0x00 }, // ""
			{ 0x78, 0x00 }, // Last game played, game ID [none]
			{ 0x79, 0x00 }, // ""
			{ 0x7A, 0x00 }, // Swan ID (see Mama Mitte) -- TODO: set to valid/random value?
			{ 0x7B, 0x00 }, // ""
			{ 0x7C, 0x00 }, // Number of different games played [none]
			{ 0x7D, 0x00 }, // Number of times settings were changed [none]
			{ 0x7E, 0x00 }, // Number of times powered on [none]
			{ 0x7F, 0x00 }  // ""
		};

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
			{ "Volume0", ("VolumeS0.png", new(0, 65)) },
			{ "Volume1", ("VolumeS1.png", new(0, 65)) },
			{ "Volume2", ("VolumeS2.png", new(0, 65)) },
			{ "Volume3", ("VolumeS3.png", new(0, 65)) },
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
