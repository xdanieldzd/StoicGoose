using System;

using StoicGoose.Common.Utilities;
using StoicGoose.Core.Interfaces;
using StoicGoose.Core.Machines;

using static StoicGoose.Common.Utilities.BitHandling;

namespace StoicGoose.Core.Cartridges
{
	/* Seiko S-3511A real-time clock, through Bandai 2003 mapper
	 * - https://forums.nesdev.org/viewtopic.php?t=21513
	 * - https://datasheetspdf.com/pdf-file/1087347/Seiko/S-3511A/1
	 */

	// TODO: interrupts, save/load current state

	public sealed class RTC : IPortAccessComponent
	{
		const int cyclesInSecond = (int)MachineCommon.CpuClock;

		readonly byte[] numPayloadBytes = new byte[] { 0, 1, 7, 3, 2, 2, 2 };
		readonly int[] numDaysPerMonth = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

		/* WS - Status & data registers */
		byte wsData;
		byte payloadIndex;

		/* WS+RTC - Communication */
		byte command;
		bool isReadAccess;

		/* RTC - Real-time data register */
		byte year, month, day, dayOfWeek, hour, minute, second;
		bool isPm, isTestModeActive;

		/* RTC - Status register */
		bool isPowered, is24HourMode;
		bool intAE, intME, intFE;

		/* RTC - Alarm time/frequency duty setting register */
		ushort intRegister;

		(bool pm, byte hour, byte minute) alarmTime => (IsBitSet((byte)(intRegister >> 0), 7), (byte)((intRegister >> 0) & 0b00111111), (byte)((intRegister >> 8) & 0b01111111));
		int selectedInterruptFreq
		{
			get
			{
				var freq = 0;
				for (var j = 0; j < 16; j++) if (((intRegister >> j) & 0b1) == 0b1) freq |= 32768 >> j;
				return freq;
			}
		}

		int cycleCount;

		public RTC()
		{
			//
		}

		public void Reset()
		{
			wsData = 0;
			payloadIndex = 0;

			command = 0;
			isReadAccess = false;

			year = dayOfWeek = hour = minute = second = 0;
			month = day = 1;
			isPm = isTestModeActive = false;

			is24HourMode = intAE = intME = false;
			isPowered = intFE = true;

			intRegister = 0x8000;

			cycleCount = 0;
		}

		public void Shutdown()
		{
			//
		}

		public void Program(DateTime dateTime)
		{
			year = (byte)(dateTime.Year % 100);
			month = (byte)(dateTime.Month % 13);
			day = (byte)(dateTime.Day % 32);
			dayOfWeek = (byte)((int)dateTime.DayOfWeek % 8);
			hour = (byte)(dateTime.Hour % 25);
			minute = (byte)(dateTime.Minute % 60);
			second = (byte)(dateTime.Second % 60);
		}

		public bool Step(int clockCyclesInStep)
		{
			var interrupt = false;

			for (var i = 0; i < clockCyclesInStep; i++)
			{
				if (intFE && !intME)
				{
					/* Selected frequency steady interrupt output */

					// TODO probably not right
					if (cycleCount >= selectedInterruptFreq)
						interrupt = true;

				}
				else if (!intFE && intME)
				{
					/* Per-minute edge interrupt output */
					// TODO
				}
				else if (intFE && intME)
				{
					/* Per-minute steady interrupt output */
					// TODO
				}
				else if (!intFE && !intME && intAE)
				{
					/* Alarm interrupt output */
					if (alarmTime.pm == isPm && Bcd.BcdToDecimal(alarmTime.hour) == hour && Bcd.BcdToDecimal(alarmTime.minute) == minute)
						interrupt = true;
				}

				cycleCount++;
				if (cycleCount >= cyclesInSecond)
				{
					UpdateClock();
					cycleCount = 0;
				}
			}

			return interrupt;
		}

		private void UpdateClock()
		{
			second++;
			if (second < 60) return;

			second = 0;
			minute++;
			if (minute < 60) return;

			minute = 0;
			hour++;
			if (hour < 24) return;

			hour = 0;
			dayOfWeek++;
			dayOfWeek %= 7;

			day++;
			var extraDay = (month == 2 && (year % 4) == 0 && ((year % 100) != 0 || (year % 400) == 0)) ? 1 : 0;
			if (day < numDaysPerMonth[month] + extraDay) return;

			day = 0;
			month++;
			if (month < 12) return;

			month = 0;
			year++;
		}

		private void PerformAccess()
		{
			switch (command & 0b111)
			{
				case 0b000:
					/* Reset */
					wsData = 0;

					year = 0;
					month = 1;
					day = 1;
					dayOfWeek = 0;
					hour = 0;
					minute = 0;
					second = 0;

					isPowered = is24HourMode = false;
					intAE = intME = intFE = false;

					intRegister = 0x0000;
					break;

				case 0b001:
					/* Status register access */
					if (isReadAccess)
					{
						wsData = 0;
						ChangeBit(ref wsData, 7, isPowered);
						ChangeBit(ref wsData, 6, is24HourMode);
						ChangeBit(ref wsData, 5, intAE);
						ChangeBit(ref wsData, 3, intME);
						ChangeBit(ref wsData, 1, intFE);
					}
					else
					{
						is24HourMode = IsBitSet(wsData, 6);
						intAE = IsBitSet(wsData, 5);
						intME = IsBitSet(wsData, 3);
						intFE = IsBitSet(wsData, 1);
					}
					break;

				case 0b010:
					/* Real-time data access 1 */
					if (isReadAccess)
					{
						wsData = 0;
						switch (payloadIndex)
						{
							case 0: wsData = (byte)Bcd.DecimalToBcd(year); break;
							case 1: wsData = (byte)Bcd.DecimalToBcd(month); break;
							case 2: wsData = (byte)Bcd.DecimalToBcd(day); break;
							case 3: wsData = (byte)Bcd.DecimalToBcd(dayOfWeek); break;
							case 4: wsData = (byte)Bcd.DecimalToBcd(hour); ChangeBit(ref wsData, 7, isPm); break;
							case 5: wsData = (byte)Bcd.DecimalToBcd(minute); break;
							case 6: wsData = (byte)Bcd.DecimalToBcd(second); ChangeBit(ref wsData, 7, isTestModeActive); break;
						}
					}
					else
					{
						switch (payloadIndex)
						{
							case 0: year = (byte)Bcd.BcdToDecimal(wsData); break;
							case 1: month = (byte)Bcd.BcdToDecimal(wsData); break;
							case 2: day = (byte)Bcd.BcdToDecimal(wsData); break;
							case 3: dayOfWeek = (byte)Bcd.BcdToDecimal(wsData); break;
							case 4: hour = (byte)(Bcd.BcdToDecimal(wsData) & 0b01111111); isPm = IsBitSet(wsData, 7); break;
							case 5: minute = (byte)Bcd.BcdToDecimal(wsData); break;
							case 6: second = (byte)(Bcd.BcdToDecimal(wsData) & 0b01111111); isTestModeActive = IsBitSet(wsData, 7); break;
						}
					}
					break;

				case 0b011:
					/* Real-time data access 2 */
					if (isReadAccess)
					{
						wsData = 0;
						switch (payloadIndex)
						{
							case 0: wsData = (byte)Bcd.DecimalToBcd(hour); ChangeBit(ref wsData, 7, isPm); break;
							case 1: wsData = (byte)Bcd.DecimalToBcd(minute); break;
							case 2: wsData = (byte)Bcd.DecimalToBcd(second); ChangeBit(ref wsData, 7, isTestModeActive); break;
						}
					}
					else
					{
						switch (payloadIndex)
						{
							case 0: hour = (byte)(Bcd.BcdToDecimal(wsData) & 0b01111111); isPm = IsBitSet(wsData, 7); break;
							case 1: minute = (byte)Bcd.BcdToDecimal(wsData); break;
							case 2: second = (byte)(Bcd.BcdToDecimal(wsData) & 0b01111111); isTestModeActive = IsBitSet(wsData, 7); break;
						}
					}
					break;

				case 0b100:
					/* Alarm time/frequency duty setting */
					if (isReadAccess)
					{
						wsData = 0;
						switch (payloadIndex)
						{
							case 0: wsData = (byte)((intRegister >> 0) & 0xFF); break;
							case 1: wsData = (byte)((intRegister >> 8) & 0xFF); break;
						}
					}
					else
					{
						switch (payloadIndex)
						{
							case 0: intRegister = (ushort)((intRegister & 0xFF00) | (wsData << 0)); break;
							case 1: intRegister = (ushort)((intRegister & 0x00FF) | (wsData << 8)); break;
						}
					}
					break;

				case 0b101:
					/* Unknown/invalid */
					if (isReadAccess)
					{
						wsData = 0xFF;
					}
					break;

				case 0b110:
					/* Test mode start -- ignored */
					break;

				case 0b111:
					/* Test mode end -- ignored */
					break;

				default:
					break;
			}
		}

		public byte ReadPort(ushort port)
		{
			var retVal = (byte)0x90;

			if (port == 0)
			{
				PerformAccess();

				payloadIndex++;

				ChangeBit(ref retVal, 7, true); // TODO: correct?
				ChangeBit(ref retVal, 4, payloadIndex < numPayloadBytes[command & 0b111]);
				ChangeBit(ref retVal, 0, true);
				retVal |= (byte)((command & 0b1111) << 1);

				if (payloadIndex >= numPayloadBytes[command & 0b111])
					payloadIndex = 0;
			}
			else if (port == 1)
			{
				retVal = wsData;
			}

			return retVal;
		}

		public void WritePort(ushort port, byte value)
		{
			if (port == 0)
			{
				isReadAccess = IsBitSet(value, 0);
				command = (byte)((value >> 1) & 0b111);

				PerformAccess();
			}
			else if (port == 1)
			{
				wsData = value;
			}
		}
	}
}
