using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace WlanProfileViewer.Models
{
	/// <summary>
	/// Loads/Saves this application's window size and position.
	/// </summary>
	/// <remarks>This class must be public because the instance will be handled by XmlSerializer.</remarks>
	public class WindowPlacement
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		private static extern bool SetWindowPlacement(
			IntPtr hWnd,
			[In] ref WINDOWPLACEMENT lpwndpl);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern bool GetWindowPlacement(
			IntPtr hWnd,
			out WINDOWPLACEMENT lpwndpl);

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct WINDOWPLACEMENT
		{
			public int length;
			public int flags;
			public SW showCmd;
			public POINT ptMinPosition;
			public POINT ptMaxPosition;
			public RECT rcNormalPosition;
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int x;
			public int y;
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		public enum SW
		{
			SW_HIDE = 0,
			SW_SHOWNORMAL = 1,
			SW_SHOWMINIMIZED = 2,
			SW_SHOWMAXIMIZED = 3,
			SW_SHOWNOACTIVATE = 4,
			SW_SHOW = 5,
			SW_MINIMIZE = 6,
			SW_SHOWMINNOACTIVE = 7,
			SW_SHOWNA = 8,
			SW_RESTORE = 9,
			SW_SHOWDEFAULT = 10,
		}

		#endregion

		public void Load(Window window)
		{
			if (Settings.Current.Placement == null)
				return;

			var handle = new WindowInteropHelper(window).Handle;

			var placement = (WINDOWPLACEMENT)Settings.Current.Placement;

			placement.length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
			placement.flags = 0; // No flag set
			placement.showCmd = SW.SW_SHOWNORMAL; // Make window state normal by default.

			SetWindowPlacement(handle, ref placement);
		}

		public void Save(Window window)
		{
			var handle = new WindowInteropHelper(window).Handle;

			WINDOWPLACEMENT placement;
			GetWindowPlacement(handle, out placement);

			Settings.Current.Placement = placement;
		}
	}
}