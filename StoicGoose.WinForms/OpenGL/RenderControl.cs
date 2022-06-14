using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.WinForms;

namespace StoicGoose.WinForms.OpenGL
{
	public class RenderControl : GLControl
	{
		readonly static DebugProc debugCallback = GLDebugCallback;
		static GCHandle debugCallbackHandle;

		bool wasShown = false;

		public RenderControl() : this(GLControlSettings.Default) { }

		public RenderControl(GLControlSettings settings) : base(settings)
		{
			Application.Idle += (s, e) =>
			{
				if (HasValidContext)
					Invalidate();
			};
		}

		protected override bool IsInputKey(Keys keyData)
		{
			return keyData switch
			{
				Keys.Right or Keys.Left or Keys.Up or Keys.Down or Keys.Shift | Keys.Right or Keys.Shift | Keys.Left or Keys.Shift | Keys.Up or Keys.Shift | Keys.Down => true,
				_ => base.IsInputKey(keyData),
			};
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (debugCallbackHandle.IsAllocated)
				debugCallbackHandle.Free();

			base.OnHandleDestroyed(e);
		}

		protected override void OnLoad(EventArgs e)
		{
			if ((Flags & ContextFlags.Debug) == ContextFlags.Debug)
			{
				debugCallbackHandle = GCHandle.Alloc(debugCallback);
				GL.DebugMessageCallback(debugCallback, IntPtr.Zero);
				GL.DebugMessageControl(DebugSourceControl.DontCare, DebugTypeControl.DontCare, DebugSeverityControl.DontCare, 0, (int[])null, true);

				GL.Enable(EnableCap.DebugOutput);
				GL.Enable(EnableCap.DebugOutputSynchronous);
			}

			base.OnLoad(e);
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (HasValidContext && !Context.IsCurrent)
				MakeCurrent();

			if (!wasShown)
			{
				OnResize(EventArgs.Empty);
				wasShown = true;
			}

			SwapBuffers();

			base.OnPaint(e);
		}

		private static void GLDebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			var messageString = Marshal.PtrToStringAnsi(message, length);
			Debug.Print($"{(type == DebugType.DebugTypeError ? "GL ERROR" : "GL callback")}: source={source}, type={type}, severity={severity}, message={messageString}");
			if (type == DebugType.DebugTypeError) throw new Exception(messageString);
		}
	}
}
