using System.Collections.Generic;

using StoicGoose.Emulation.Display;
using StoicGoose.DataStorage;

namespace StoicGoose.Emulation.Machines
{
	public partial class WonderSwan
	{
		public static ObjectStorage Metadata { get; } = new ObjectStorage();

		static WonderSwan()
		{
			Metadata["machine/description/manufacturer"].Value = "Bandai";
			Metadata["machine/description/model"].Value = "WonderSwan";

			Metadata["machine/display/width"].Value = DisplayController.ScreenWidth;
			Metadata["machine/display/height"].Value = DisplayController.ScreenHeight;
			Metadata["machine/display/refresh"].Value = DisplayController.VerticalClock;

			Metadata["machine/input/controls"].Value = "start, b, a, x1, x2, x3, x4, y1, y2, y3, y4";
			Metadata["machine/input/hardware"].Value = "volume";

			Metadata["interface/files/romfilter"].Value = "WonderSwan ROMs (*.ws;*.wsc)|*.ws;*.wsc";

			Metadata["interface/icons/size"].Value = 12;
			Metadata["interface/icons/power/resource"].Value = "Power.png";
			Metadata["interface/icons/power/location"].Value = "0, 144";
			Metadata["interface/icons/initialized/resource"].Value = "Initialized.png";
			Metadata["interface/icons/initialized/location"].Value = "18, 144";
			Metadata["interface/icons/sleep/resource"].Value = "Sleep.png";
			Metadata["interface/icons/sleep/location"].Value = "50, 144";
			Metadata["interface/icons/lowbatt/resource"].Value = "LowBattery.png";
			Metadata["interface/icons/lowbatt/location"].Value = "80, 144";
			Metadata["interface/icons/volume0/resource"].Value = "Volume0.png";
			Metadata["interface/icons/volume0/location"].Value = "105, 144";
			Metadata["interface/icons/volume1/resource"].Value = "Volume1.png";
			Metadata["interface/icons/volume1/location"].Value = "105, 144";
			Metadata["interface/icons/volume2/resource"].Value = "Volume2.png";
			Metadata["interface/icons/volume2/location"].Value = "105, 144";
			Metadata["interface/icons/headphones/resource"].Value = "Headphones.png";
			Metadata["interface/icons/headphones/location"].Value = "130, 144";
			Metadata["interface/icons/horizontal/resource"].Value = "Horizontal.png";
			Metadata["interface/icons/horizontal/location"].Value = "155, 144";
			Metadata["interface/icons/vertical/resource"].Value = "Vertical.png";
			Metadata["interface/icons/vertical/location"].Value = "168, 144";
			Metadata["interface/icons/aux1/resource"].Value = "Aux1.png";
			Metadata["interface/icons/aux1/location"].Value = "185, 144";
			Metadata["interface/icons/aux2/resource"].Value = "Aux2.png";
			Metadata["interface/icons/aux2/location"].Value = "195, 144";
			Metadata["interface/icons/aux3/resource"].Value = "Aux3.png";
			Metadata["interface/icons/aux3/location"].Value = "205, 144";
		}

		public void UpdateMetadata()
		{
			// icons
			var icons = new List<string>();
			if (true) icons.Add("power");
			if (hwSelfTestOk) icons.Add("initialized");  //???
			icons.AddRange(display.GetActiveIcons());
			icons.AddRange(sound.GetActiveIcons());
			Metadata["machine/display/icons/active"].Value = string.Join(",", icons);
		}
	}
}
