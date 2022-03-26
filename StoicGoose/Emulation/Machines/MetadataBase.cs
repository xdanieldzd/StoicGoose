using System.Collections.Generic;

using OpenTK.Mathematics;

namespace StoicGoose.Emulation.Machines
{
	public abstract class MetadataBase
	{
		public abstract string Manufacturer { get; }
		public abstract string Model { get; }
		public abstract Vector2i ScreenSize { get; }
		public abstract double RefreshRate { get; }
		public abstract string GameControls { get; }
		public abstract string HardwareControls { get; }
		public abstract string InternalEepromFilename { get; }
		public abstract string RomFileFilter { get; }
		public abstract int StatusIconSize { get; }
		public abstract Dictionary<string, (string filename, Vector2i location)> StatusIcons { get; }

		public Dictionary<string, bool> IsStatusIconActive { get; } = new();
	}
}
