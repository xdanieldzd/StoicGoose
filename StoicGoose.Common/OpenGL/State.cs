using System;
using System.Drawing;
using System.Runtime.CompilerServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace StoicGoose.Common.OpenGL
{
	public class State
	{
		static State lastState = default;

		bool depthTestEnable = true, blendEnable = true, cullFaceEnable = true, scissorTestEnable = false;
		BlendingFactor blendSource = BlendingFactor.SrcAlpha, blendDest = BlendingFactor.OneMinusSrcAlpha;
		CullFaceMode cullFaceMode = CullFaceMode.Back;
		Vector4i scissorBox = Vector4i.Zero;
		Color clearColor = Color.Black;
		Vector4i viewport = Vector4i.Zero;

		public void Enable(EnableCap cap) => SetCap(cap, true);
		public void Disable(EnableCap cap) => SetCap(cap, false);

		public void SetBlending(BlendingFactor source, BlendingFactor dest) { blendSource = source; blendDest = dest; }
		public void SetCullFace(CullFaceMode mode) => cullFaceMode = mode;
		public void SetScissor(Vector4i box) => scissorBox = box;
		public void SetScissor(int x, int y, int width, int height) => scissorBox = new(x, y, width, height);
		public void SetClearColor(Color color) => clearColor = color;
		public void SetViewport(Vector4i vp) => viewport = vp;
		public void SetViewport(int x, int y, int width, int height) => viewport = new(x, y, width, height);

		private void SetCap(EnableCap cap, bool value)
		{
			switch (cap)
			{
				case EnableCap.DepthTest: depthTestEnable = value; break;
				case EnableCap.Blend: blendEnable = value; break;
				case EnableCap.CullFace: cullFaceEnable = value; break;
				case EnableCap.ScissorTest: scissorTestEnable = value; break;
				default: throw new StateException($"{cap} not implemented");
			}
		}

		private bool GetCap(EnableCap cap)
		{
			return cap switch
			{
				EnableCap.DepthTest => depthTestEnable,
				EnableCap.Blend => blendEnable,
				EnableCap.CullFace => cullFaceEnable,
				EnableCap.ScissorTest => scissorTestEnable,
				_ => throw new StateException($"{cap} not implemented"),
			};
		}

		public void Submit()
		{
			if (lastState?.clearColor != clearColor)
				GL.ClearColor(clearColor);

			SubmitState(EnableCap.DepthTest, depthTestEnable);
			SubmitState(EnableCap.Blend, blendEnable);
			SubmitState(EnableCap.CullFace, cullFaceEnable);
			SubmitState(EnableCap.ScissorTest, scissorTestEnable);

			if (lastState?.viewport != viewport)
				GL.Viewport(viewport.X, viewport.Y, viewport.Z, viewport.W);

			lastState = (State)MemberwiseClone();
		}

		private void SubmitState(EnableCap cap, bool value)
		{
			var enableChanged = lastState?.GetCap(cap) != GetCap(cap);

			if (value)
			{
				if (enableChanged) GL.Enable(cap);

				switch (cap)
				{
					case EnableCap.Blend:
						if (lastState?.blendSource != blendSource || lastState?.blendDest != blendDest)
							GL.BlendFunc(blendSource, blendDest);
						break;

					case EnableCap.CullFace:
						if (lastState?.cullFaceMode != cullFaceMode)
							GL.CullFace(cullFaceMode);
						break;

					case EnableCap.ScissorTest:
						if (lastState?.scissorBox != scissorBox)
							GL.Scissor(scissorBox.X, scissorBox.Y, scissorBox.Z, scissorBox.W);
						break;
				}
			}
			else
			{
				if (enableChanged) GL.Disable(cap);
			}
		}
	}

	public class StateException : Exception
	{
		public StateException(string message, [CallerMemberName] string callerName = "") : base($"In {callerName}: {message}") { }
	}
}
