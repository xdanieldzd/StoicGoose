using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StoicGoose.WinForms.Controls
{
	public class TreeViewEx : TreeView
	{
		[DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
		private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

		protected override void OnHandleCreated(EventArgs e)
		{
			if (!DesignMode && Environment.OSVersion?.Platform == PlatformID.Win32NT && Environment.OSVersion?.Version.Major >= 6)
				_ = SetWindowTheme(Handle, "explorer", null);

			base.OnHandleCreated(e);
		}
	}
}
