using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace StoicGoose.XInput
{
	static class NativeMethods
	{
		const string dllName = "xinput9_1_0.dll";

		public const int FlagGamepad = 0x00000001;

		[DllImport(dllName, EntryPoint = "XInputGetState")]
		public static extern int GetState(int dwUserIndex, ref XInputState pState);
		[DllImport(dllName, EntryPoint = "XInputSetState")]
		public static extern int SetState(int dwUserIndex, ref XInputVibration pVibration);
		[DllImport(dllName, EntryPoint = "XInputGetCapabilities")]
		public static extern int GetCapabilities(int dwUserIndex, int dwFlags, ref XInputCapabilities pCapabilities);
	}

	public enum Errors
	{
		Success = 0x00000000,
		BadArguments = 0x000000A0,
		DeviceNotConnected = 0x0000048F
	}

	/* https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.directx_sdk.reference.xinput_gamepad%28v=vs.85%29.aspx */
	[StructLayout(LayoutKind.Explicit)]
	public struct XInputGamepad
	{
		[FieldOffset(0)]
		readonly ushort wButtons;
		[FieldOffset(2)]
		public byte bLeftTrigger;
		[FieldOffset(3)]
		public byte bRightTrigger;
		[FieldOffset(4)]
		public short sThumbLX;
		[FieldOffset(6)]
		public short sThumbLY;
		[FieldOffset(8)]
		public short sThumbRX;
		[FieldOffset(10)]
		public short sThumbRY;

		public const int LeftThumbDeadzone = 7849;
		public const int RightThumbDeadzone = 8689;
		public const int TriggerThreshold = 30;

		public Buttons Buttons => (Buttons)wButtons;
	}

	[Flags]
	public enum Buttons
	{
		[Description("None")]
		None = 0x0000,
		[Description("D-Pad Up")]
		DPadUp = 0x0001,
		[Description("D-Pad Down")]
		DPadDown = 0x0002,
		[Description("D-Pad Left")]
		DPadLeft = 0x0004,
		[Description("D-Pad Right")]
		DPadRight = 0x0008,
		[Description("Start")]
		Start = 0x0010,
		[Description("Back")]
		Back = 0x0020,
		[Description("Left Thumbstick")]
		LeftThumb = 0x0040,
		[Description("Right Thumbstick")]
		RightThumb = 0x0080,
		[Description("Left Shoulder")]
		LeftShoulder = 0x0100,
		[Description("Right Shoulder")]
		RightShoulder = 0x0200,
		[Description("A")]
		A = 0x1000,
		[Description("B")]
		B = 0x2000,
		[Description("X")]
		X = 0x4000,
		[Description("Y")]
		Y = 0x8000
	}

	/* https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.directx_sdk.reference.xinput_state%28v=vs.85%29.aspx */
	[StructLayout(LayoutKind.Explicit)]
	public struct XInputState
	{
		[FieldOffset(0)]
		public uint dwPacketNumber;
		[FieldOffset(4)]
		public XInputGamepad Gamepad;
	}

	/* https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.directx_sdk.reference.xinput_vibration%28v=vs.85%29.aspx */
	[StructLayout(LayoutKind.Explicit)]
	public struct XInputVibration
	{
		[FieldOffset(0)]
		public ushort wLeftMotorSpeed;
		[FieldOffset(2)]
		public ushort wRightMotorSpeed;
	}

	/* https://msdn.microsoft.com/en-us/library/windows/desktop/microsoft.directx_sdk.reference.xinput_capabilities%28v=vs.85%29.aspx */
	[StructLayout(LayoutKind.Explicit)]
	public struct XInputCapabilities
	{
		[FieldOffset(0)]
		readonly byte type;
		[FieldOffset(1)]
		readonly byte subType;
		[FieldOffset(2)]
		readonly ushort flags;
		[FieldOffset(4)]
		public XInputGamepad Gamepad;
		[FieldOffset(16)]
		public XInputVibration Vibration;

		public DeviceType Type => (DeviceType)type;
		public DeviceSubType SubType => (DeviceSubType)subType;
		public DeviceFlags Flags => (DeviceFlags)flags;
	}

	public enum DeviceType
	{
		Gamepad = 0x01
	}

	public enum DeviceSubType
	{
		Gamepad = 0x01,
		Wheel = 0x02,
		ArcadeStick = 0x03,
		FlightStick = 0x04,
		DancePad = 0x05,
		Guitar = 0x06,
		DrumKit = 0x08
	}

	[Flags]
	public enum DeviceFlags
	{
		VoiceSupported = 0x0004
	}
}
