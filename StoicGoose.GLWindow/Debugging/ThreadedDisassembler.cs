using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Gee.External.Capstone;
using Gee.External.Capstone.X86;

using StoicGoose.Common.Console;
using StoicGoose.Core;

namespace StoicGoose.GLWindow.Debugging
{
	public class ThreadedDisassembler
	{
		/* Constants, etc. */
		const int segmentSize = 0x10000;

		/* Capstone disassembler */
		readonly CapstoneX86Disassembler disassembler = new(X86DisassembleMode.Bit16)
		{
			DisassembleSyntax = DisassembleSyntax.Intel,
			EnableInstructionDetails = true,
			EnableSkipDataMode = true
		};

		/* Segment cache and memory access */
		readonly Dictionary<ushort, List<Instruction>> disassembledSegmentCache = new();
		public MemoryReadDelegate ReadDelegate { get; set; }

		/* Threading */
		int threadsCount = 0, threadsCompletedCount = 0;
		bool allThreadsRan = false;

		/* Con-/destructors */
		public ThreadedDisassembler() => ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Disassembler initialized.");
		~ThreadedDisassembler() => disassembler.Dispose();

		public Dictionary<ushort, List<Instruction>> GetSegmentCache() => disassembledSegmentCache;
		public void SetSegmentCache(Dictionary<ushort, List<Instruction>> cache)
		{
			cache.ToList().ForEach(x => disassembledSegmentCache[x.Key] = x.Value);
			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, $"{disassembledSegmentCache.Count} segment(s) restored.");
		}

		public void Start(params ushort[] segments)
		{
			segments = segments.Where(x => x != 0xFFFF && x != 0xFE00).ToArray(); /* Filter out CS startup value & bootstrap segment */

			allThreadsRan = false;
			threadsCount += segments.Length;
			for (var i = 0; i < segments.Length; i++)
				ThreadPool.QueueUserWorkItem(ThreadFunc, segments[i]);
		}

		public void Clear()
		{
			disassembledSegmentCache.Clear();
			ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, "Cache cleared.");
		}

		public List<Instruction> GetSegmentInstructions(ushort segment)
		{
			if (disassembledSegmentCache.ContainsKey(segment))
				return disassembledSegmentCache[segment];

			Start(segment);

			return null;
		}

		private void ThreadFunc(object context)
		{
			if (context is ushort segment && !disassembledSegmentCache.ContainsKey(segment))
			{
				var instructions = new List<Instruction>();
				var data = new byte[segmentSize];
				for (var i = 0; i < data.Length; i++) data[i] = ReadDelegate((uint)((segment << 4) + i));

				var disassemblerOutput = disassembler.Disassemble(data, 0);
				instructions.AddRange(disassemblerOutput.Select(x => new Instruction(x)));

				if (disassembledSegmentCache.TryAdd(segment, instructions))
					ConsoleHelpers.WriteLog(ConsoleLogSeverity.Information, this, $"Disassembled segment 0x{segment:X4}, {instructions.Count} instruction(s).");
			}

			if (Interlocked.Increment(ref threadsCompletedCount) >= threadsCount && !allThreadsRan)
			{
				allThreadsRan = true;
				ConsoleHelpers.WriteLog(ConsoleLogSeverity.Success, this, $"{disassembledSegmentCache.Count} segment(s) in cache.");
			}
		}
	}
}
