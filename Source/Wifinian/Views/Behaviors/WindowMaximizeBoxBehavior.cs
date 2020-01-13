using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Xaml.Behaviors;

namespace Wifinian.Views.Behaviors
{
	[TypeConstraint(typeof(Window))]
	public class WindowMaximizeBoxBehavior : Behavior<Window>
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		private static extern int GetWindowLong(
			IntPtr hWnd,
			int nIndex);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr GetWindowLongPtr(
			IntPtr hWnd,
			int nIndex);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern int SetWindowLong(
			IntPtr hWnd,
			int nIndex,
			int dwNewLong);

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr SetWindowLongPtr(
			IntPtr hWnd,
			int nIndex,
			IntPtr dwNewLong);

		private const int GWL_STYLE = -16;
		private const int WS_MAXIMIZEBOX = 0x00010000;

		#endregion

		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += OnLoaded;
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.Loaded -= OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			DisableMaximizeBox(this.AssociatedObject);
		}

		private void DisableMaximizeBox(Window window)
		{
			var windowHandle = new WindowInteropHelper(window).Handle;

			if (!Environment.Is64BitProcess)
			{
				// These functions are for 32-bit process (not necessarily 32-bit OS). Actually, they seem to work
				// on 64-bit process as well.
				var value1 = GetWindowLong(windowHandle, GWL_STYLE);
				if (value1 == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				var value2 = SetWindowLong(windowHandle, GWL_STYLE, (value1 & ~WS_MAXIMIZEBOX));
				if (value2 == 0)
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
			else
			{
				// These functions are for 64-bit process.
				var value1 = GetWindowLongPtr(windowHandle, GWL_STYLE);
				if (value1 == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error());

				var value2 = SetWindowLongPtr(windowHandle, GWL_STYLE, (IntPtr)(value1.ToInt32() & ~WS_MAXIMIZEBOX));
				if (value2 == IntPtr.Zero)
					throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}
	}
}