using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace WlanProfileViewer.Views
{
	public static class WindowPosition
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowPos(
			IntPtr hWnd,
			IntPtr hWndInsertAfter,
			int X,
			int Y,
			int cx,
			int cy,
			SWP uFlags);

		private enum SWP : uint
		{
			SWP_ASYNCWINDOWPOS = 0x4000,
			SWP_DEFERERASE = 0x2000,
			SWP_DRAWFRAME = 0x0020,
			SWP_FRAMECHANGED = 0x0020,
			SWP_HIDEWINDOW = 0x0080,
			SWP_NOACTIVATE = 0x0010,
			SWP_NOCOPYBITS = 0x0100,
			SWP_NOMOVE = 0x0002,
			SWP_NOOWNERZORDER = 0x0200,
			SWP_NOREDRAW = 0x0008,
			SWP_NOREPOSITION = 0x0200,
			SWP_NOSENDCHANGING = 0x0400,
			SWP_NOSIZE = 0x0001,
			SWP_NOZORDER = 0x0004,
			SWP_SHOWWINDOW = 0x0040,
		}

		[DllImport("Shell32.dll", SetLastError = true)]
		private static extern IntPtr SHAppBarMessage(
			ABM dwMessage,
			ref APPBARDATA pData);

		[StructLayout(LayoutKind.Sequential)]
		private struct APPBARDATA
		{
			public uint cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public ABE uEdge;
			public RECT rc;
			public int lParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public static implicit operator Rect(RECT rect)
			{
				if ((rect.right - rect.left < 0) || (rect.bottom - rect.top < 0))
					return Rect.Empty;

				return new Rect(
					rect.left,
					rect.top,
					rect.right - rect.left,
					rect.bottom - rect.top);
			}
		}

		private enum ABM : uint
		{
			ABM_NEW = 0x00000000,
			ABM_REMOVE = 0x00000001,
			ABM_QUERYPOS = 0x00000002,
			ABM_SETPOS = 0x00000003,
			ABM_GETSTATE = 0x00000004,
			ABM_GETTASKBARPOS = 0x00000005,
			ABM_ACTIVATE = 0x00000006,
			ABM_GETAUTOHIDEBAR = 0x00000007,
			ABM_SETAUTOHIDEBAR = 0x00000008,
			ABM_WINDOWPOSCHANGE = 0x00000009,
			ABM_SETSTATE = 0x0000000A,
		}

		private enum ABE : uint
		{
			ABE_LEFT = 0,
			ABE_TOP = 1,
			ABE_RIGHT = 2,
			ABE_BOTTOM = 3
		}

		[DllImport("Shell32.dll", SetLastError = true)]
		private static extern int Shell_NotifyIconGetRect(
			[In] ref NOTIFYICONIDENTIFIER identifier,
			out RECT iconLocation);

		[StructLayout(LayoutKind.Sequential)]
		private struct NOTIFYICONIDENTIFIER
		{
			public uint cbSize;
			public IntPtr hWnd;
			public uint uID;
			public GUID guidItem; // System.Guid can be used.
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct GUID
		{
			public uint Data1;
			public ushort Data2;
			public ushort Data3;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
			public byte[] Data4;
		}

		private const int S_OK = 0x00000000;
		private const int S_FALSE = 0x00000001;

		#endregion

		#region Window

		public static bool SetWindowLocation(Window window, Point location)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			return SetWindowPos(
				windowHandle,
				new IntPtr(-1), // HWND_TOPMOST
				(int)location.X,
				(int)location.Y,
				0,
				0,
				SWP.SWP_NOSIZE);
		}

		#endregion

		#region Taskbar

		public static TaskbarAlignment GetTaskbarAlignment()
		{
			var data = new APPBARDATA
			{
				cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
			};

			var result = SHAppBarMessage(
				ABM.ABM_GETTASKBARPOS,
				ref data);

			return (result != IntPtr.Zero)
				? ConvertToTaskbarAlignment(data.uEdge)
				: TaskbarAlignment.None;
		}

		private static TaskbarAlignment ConvertToTaskbarAlignment(ABE value)
		{
			switch (value)
			{
				case ABE.ABE_LEFT:
					return TaskbarAlignment.Left;
				case ABE.ABE_TOP:
					return TaskbarAlignment.Top;
				case ABE.ABE_RIGHT:
					return TaskbarAlignment.Right;
				case ABE.ABE_BOTTOM:
					return TaskbarAlignment.Bottom;
				default:
					throw new NotSupportedException("The value is unknown.");
			}
		}

		#endregion

		#region NotifyIcon

		/// <summary>
		/// Get the rectangle of a specified NotifyIcon.
		/// </summary>
		/// <param name="notifyIcon">NotifyIcon</param>
		/// <returns>Rectangle of the NotifyIcon</returns>
		/// <remarks>
		/// The idea to get the rectangle of a NotifyIcon is derived from:
		/// https://github.com/rzhw/SuperNotifyIcon
		/// </remarks>
		public static Rect GetNotifyIconRect(NotifyIcon notifyIcon)
		{
			NOTIFYICONIDENTIFIER identifier;
			if (!TryGetNotifyIconIdentifier(notifyIcon, out identifier))
				return Rect.Empty;

			RECT iconLocation;
			int result = Shell_NotifyIconGetRect(ref identifier, out iconLocation);

			Debug.WriteLine($"Shell_NotifyIconGetRect: {result}");

			switch (result)
			{
				case S_OK:
				case S_FALSE:
					return iconLocation;
				default:
					return Rect.Empty;
			}
		}

		private static bool TryGetNotifyIconIdentifier(NotifyIcon notifyIcon, out NOTIFYICONIDENTIFIER identifier)
		{
			identifier = new NOTIFYICONIDENTIFIER()
			{
				cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER))
			};

			int id;
			if (!TryGetNonPublicFieldValue(notifyIcon, "id", out id))
				return false;

			NativeWindow window;
			if (!TryGetNonPublicFieldValue(notifyIcon, "window", out window))
				return false;

			identifier.uID = (uint)id;
			identifier.hWnd = window.Handle;
			return true;
		}

		private static bool TryGetNonPublicFieldValue<T>(object instance, string fieldName, out T fieldValue)
		{
			fieldValue = default(T);

			var fieldInfo = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (fieldInfo == null)
				return false;

			var value = fieldInfo.GetValue(instance);
			if (!(value is T))
				return false;

			fieldValue = (T)value;
			return true;
		}

		#endregion
	}
}