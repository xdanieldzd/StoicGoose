using System.Collections.Generic;

using StoicGoose.Emulation.Display;

namespace StoicGoose.Emulation.Machines
{
	public partial class WonderSwan
	{
		protected override void FillMetadata()
		{
			Metadata["machine/description/manufacturer"] = "Bandai";
			Metadata["machine/description/model"] = "WonderSwan";

			Metadata["machine/display/width"] = DisplayControllerCommon.ScreenWidth;
			Metadata["machine/display/height"] = DisplayControllerCommon.ScreenHeight;
			Metadata["machine/display/refresh"] = DisplayControllerCommon.VerticalClock;

			Metadata["machine/input/controls"] = "start, b, a, x1, x2, x3, x4, y1, y2, y3, y4";
			Metadata["machine/input/hardware"] = "volume";

			Metadata["machine/eeprom/filename"] = "WonderSwan.eep";

			Metadata["interface/files/romfilter"] = "WonderSwan ROMs (*.ws)|*.ws";

			Metadata["interface/icons/size"] = 12;
			Metadata["interface/icons/power/resource"] = "Power.png";
			Metadata["interface/icons/power/location"] = "0, 144";
			Metadata["interface/icons/initialized/resource"] = "Initialized.png";
			Metadata["interface/icons/initialized/location"] = "18, 144";
			Metadata["interface/icons/sleep/resource"] = "Sleep.png";
			Metadata["interface/icons/sleep/location"] = "50, 144";
			Metadata["interface/icons/lowbatt/resource"] = "LowBattery.png";
			Metadata["interface/icons/lowbatt/location"] = "80, 144";
			Metadata["interface/icons/volume0/resource"] = "Volume0.png";
			Metadata["interface/icons/volume0/location"] = "105, 144";
			Metadata["interface/icons/volume1/resource"] = "Volume1.png";
			Metadata["interface/icons/volume1/location"] = "105, 144";
			Metadata["interface/icons/volume2/resource"] = "Volume2.png";
			Metadata["interface/icons/volume2/location"] = "105, 144";
			Metadata["interface/icons/headphones/resource"] = "Headphones.png";
			Metadata["interface/icons/headphones/location"] = "130, 144";
			Metadata["interface/icons/horizontal/resource"] = "Horizontal.png";
			Metadata["interface/icons/horizontal/location"] = "155, 144";
			Metadata["interface/icons/vertical/resource"] = "Vertical.png";
			Metadata["interface/icons/vertical/location"] = "168, 144";
			Metadata["interface/icons/aux1/resource"] = "Aux1.png";
			Metadata["interface/icons/aux1/location"] = "185, 144";
			Metadata["interface/icons/aux2/resource"] = "Aux2.png";
			Metadata["interface/icons/aux2/location"] = "195, 144";
			Metadata["interface/icons/aux3/resource"] = "Aux3.png";
			Metadata["interface/icons/aux3/location"] = "205, 144";
		}

		public override void UpdateMetadata()
		{
			// icons
			var icons = new List<string>();
			if (true) icons.Add("power");
			if (hwSelfTestOk) icons.Add("initialized");  //???
			icons.AddRange(display.GetActiveIcons());
			icons.AddRange(sound.GetActiveIcons());
			Metadata["machine/display/icons/active"] = string.Join(",", icons);
		}
	}
}
