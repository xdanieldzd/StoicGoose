using System.IO;
using System.Text;

namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ
	{
		StreamWriter logWriter = default;

		private void InitializeTraceLogger(string filename)
		{
			logWriter = new StreamWriter(filename, false, Encoding.ASCII, 1024 * 64);
		}

		private void WriteToTraceLog(string line)
		{
			logWriter?.WriteLine(line);
		}

		private void CloseTraceLogger()
		{
			logWriter?.Flush();
			logWriter?.Close();
		}
	}
}
