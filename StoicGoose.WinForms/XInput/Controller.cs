using System;

namespace StoicGoose.WinForms.XInput
{
	public class Controller
	{
		XInputState inputStatesCurrent;
		bool timedVibrationEnabled;
		DateTime vibrationStopTime;

		public Buttons Buttons { get; private set; }
		public ThumbstickPosition LeftThumbstick { get; private set; }
		public ThumbstickPosition RightThumbstick { get; private set; }
		public float LeftTrigger { get; private set; }
		public float RightTrigger { get; private set; }

		public bool IsConnected { get; private set; }
		public int UserIndex { get; private set; }

		public Controller(int index)
		{
			inputStatesCurrent = new();
			timedVibrationEnabled = false;
			vibrationStopTime = DateTime.Now;

			IsConnected = false;
			UserIndex = index;
		}

		public void Update()
		{
			var newInputState = new XInputState();
			var result = (Errors)NativeMethods.GetState(UserIndex, ref newInputState);
			if (result == Errors.Success)
			{
				IsConnected = true;

				inputStatesCurrent = newInputState;

				if (inputStatesCurrent.Gamepad.sThumbLX < XInputGamepad.LeftThumbDeadzone && inputStatesCurrent.Gamepad.sThumbLX > -XInputGamepad.LeftThumbDeadzone &&
					inputStatesCurrent.Gamepad.sThumbLY < XInputGamepad.LeftThumbDeadzone && inputStatesCurrent.Gamepad.sThumbLY > -XInputGamepad.LeftThumbDeadzone)
				{
					inputStatesCurrent.Gamepad.sThumbLX = inputStatesCurrent.Gamepad.sThumbLY = 0;
				}

				if (inputStatesCurrent.Gamepad.sThumbRX < XInputGamepad.RightThumbDeadzone && inputStatesCurrent.Gamepad.sThumbRX > -XInputGamepad.RightThumbDeadzone &&
					inputStatesCurrent.Gamepad.sThumbRY < XInputGamepad.RightThumbDeadzone && inputStatesCurrent.Gamepad.sThumbRY > -XInputGamepad.RightThumbDeadzone)
				{
					inputStatesCurrent.Gamepad.sThumbRX = inputStatesCurrent.Gamepad.sThumbRY = 0;
				}

				if (inputStatesCurrent.Gamepad.bLeftTrigger < XInputGamepad.TriggerThreshold) inputStatesCurrent.Gamepad.bLeftTrigger = 0;
				if (inputStatesCurrent.Gamepad.bRightTrigger < XInputGamepad.TriggerThreshold) inputStatesCurrent.Gamepad.bRightTrigger = 0;

				Buttons = inputStatesCurrent.Gamepad.Buttons;
				LeftThumbstick = new(inputStatesCurrent.Gamepad.sThumbLX / 32767f, inputStatesCurrent.Gamepad.sThumbLY / 32767f);
				RightThumbstick = new(inputStatesCurrent.Gamepad.sThumbRX / 32767f, inputStatesCurrent.Gamepad.sThumbRY / 32767f);
				LeftTrigger = inputStatesCurrent.Gamepad.bLeftTrigger / 255f;
				RightTrigger = inputStatesCurrent.Gamepad.bRightTrigger / 255f;

				if (timedVibrationEnabled && DateTime.Now >= vibrationStopTime)
				{
					timedVibrationEnabled = false;
					Vibrate(0f, 0f);
				}
			}
			else if (result == Errors.DeviceNotConnected)
			{
				IsConnected = false;
			}
			else
				throw new Exception($"Error code {result}");
		}

		public bool IsDPadUpPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.DPadUp);
		public bool IsDPadDownPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.DPadDown);
		public bool IsDPadLeftPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.DPadLeft);
		public bool IsDPadRightPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.DPadRight);
		public bool IsStartPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.Start);
		public bool IsBackPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.Back);
		public bool IsLeftThumbPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.LeftThumb);
		public bool IsRightThumbPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.RightThumb);
		public bool IsLeftShoulderPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.LeftShoulder);
		public bool IsRightShoulderPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.RightShoulder);
		public bool IsAPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.A);
		public bool IsBPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.B);
		public bool IsXPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.X);
		public bool IsYPressed() => inputStatesCurrent.Gamepad.Buttons.HasFlag(Buttons.Y);

		public void Vibrate(float leftMotor, float rightMotor)
		{
			var vibrationState = new XInputVibration
			{
				wLeftMotorSpeed = (ushort)(leftMotor * 65535f),
				wRightMotorSpeed = (ushort)(rightMotor * 65535f)
			};
			_ = NativeMethods.SetState(UserIndex, ref vibrationState);
		}

		public void Vibrate(float leftMotor, float rightMotor, TimeSpan duration)
		{
			Vibrate(leftMotor, rightMotor);

			vibrationStopTime = DateTime.Now.Add(duration);
			timedVibrationEnabled = true;
		}
	}

	public class ThumbstickPosition
	{
		public static ThumbstickPosition Zero => new(0f, 0f);

		public float X { get; private set; }
		public float Y { get; private set; }

		public ThumbstickPosition(float x, float y)
		{
			if (x >= 1f) x = 1f;
			if (x < -1f) x = -1f;
			X = x;

			if (y >= 1f) y = 1f;
			if (y < -1f) y = -1f;
			Y = y;
		}

		public override bool Equals(object obj) => Equals(obj as ThumbstickPosition);

		public bool Equals(ThumbstickPosition pos)
		{
			if (pos is null || GetType() != pos.GetType()) return false;
			if (ReferenceEquals(this, pos)) return true;
			return X == pos.X && Y == pos.Y;
		}
		public override int GetHashCode() => (X, Y).GetHashCode();

		public static bool operator ==(ThumbstickPosition l, ThumbstickPosition r)
		{
			if (l is null)
			{
				if (r is null) return true;
				return false;
			}
			return l.Equals(r);
		}

		public static bool operator !=(ThumbstickPosition l, ThumbstickPosition r) => !(l == r);

		public override string ToString() => $"({X}, {Y})";
	}
}
