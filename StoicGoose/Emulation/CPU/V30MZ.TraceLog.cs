using System.IO;
using System.Text;

namespace StoicGoose.Emulation.CPU
{
	public sealed partial class V30MZ
	{
		StreamWriter logWriter = default;

		bool isTraceLogOpen => logWriter?.BaseStream != null && logWriter.BaseStream.CanWrite;

		public void InitializeTraceLogger(string filename)
		{
			logWriter = new StreamWriter(filename, false, Encoding.ASCII, 1024 * 64);
		}

		private void WriteToTraceLog(string line)
		{
			if (isTraceLogOpen)
				logWriter?.WriteLine(line);
		}

		public void CloseTraceLogger()
		{
			if (isTraceLogOpen)
			{
				logWriter?.Flush();
				logWriter?.Close();
			}
		}
	}
}
