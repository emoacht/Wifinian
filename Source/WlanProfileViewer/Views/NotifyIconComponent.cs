using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace WlanProfileViewer.Views
{
	public class NotifyIconComponent : Component
	{
		private readonly Window _ownerWindow;
		private readonly Container _container;
		private readonly NotifyIcon _notifyIcon;

		public NotifyIconComponent(Window ownerWindow)
		{
			if (ownerWindow == null)
				throw new ArgumentNullException(nameof(ownerWindow));

			this._ownerWindow = ownerWindow;
			this._container = new Container();
			this._notifyIcon = new NotifyIcon(this._container)
			{
				ContextMenuStrip = new ContextMenuStrip()
			};

			_notifyIcon.MouseClick += OnMouseClick;
			_notifyIcon.MouseDoubleClick += OnMouseDoubleClick;
		}

		#region Dispose

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_container.Dispose();
			}
			base.Dispose(disposing);
		}

		#endregion

		#region Property

		public System.Drawing.Icon Icon
		{
			get { return _icon; }
			set
			{
				_icon = value;
				SetIcon(value, Dpi);
			}
		}
		private System.Drawing.Icon _icon;

		public string Text
		{
			get { return _notifyIcon.Text; }
			set { _notifyIcon.Text = value; }
		}

		public float Dpi
		{
			get { return _dpi; }
			set
			{
				if (_dpi == value)
					return;

				_dpi = value;
				SetIcon(Icon, value);
			}
		}
		private float _dpi = 0F;

		#endregion

		#region Icon

		private void SetIcon(System.Drawing.Icon icon, float dpi)
		{
			if ((icon == null) || (dpi == 0F))
				return;

			var iconSize = SelectIconSize(dpi);
			_notifyIcon.Icon = new System.Drawing.Icon(icon, iconSize);
		}

		private const float _defaultDpi = 96F;
		private const float _limit16 = 1.1F; // Upper limit (110%) for 16x16
		private const float _limit32 = 2.0F; // Upper limit (200%) for 32x32

		private static System.Drawing.Size SelectIconSize(float dpi)
		{
			var factor = dpi / _defaultDpi;
			if (factor <= _limit16)
			{
				return new System.Drawing.Size(16, 16);
			}
			if (factor <= _limit32)
			{
				return new System.Drawing.Size(32, 32);
			}
			return new System.Drawing.Size(48, 48);
		}

		public void ShowIcon(string iconPath, string text = null, float dpi = _defaultDpi)
		{
			if (string.IsNullOrWhiteSpace(iconPath))
				throw new ArgumentNullException(nameof(iconPath));

			var iconResource = System.Windows.Application.GetResourceStream(new Uri(iconPath));
			if (iconResource != null)
			{
				using (var iconStream = iconResource.Stream)
				{
					var icon = new System.Drawing.Icon(iconStream);
					ShowIcon(icon, text, dpi);
				}
			}
		}

		public void ShowIcon(System.Drawing.Icon icon, string text = null, float dpi = _defaultDpi)
		{
			if (icon == null)
				throw new ArgumentNullException(nameof(icon));

			if (this.Icon != null)
				return;

			this.Icon = icon;
			this.Text = text ?? _ownerWindow.GetType().Assembly.GetName().Name;
			this.Dpi = dpi;

			_notifyIcon.Visible = true;
		}

		#endregion

		#region Click

		private void OnMouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				ShowNotifyWindow();
			}
			else
			{
				ShowOwnerWindow();
			}
		}

		private void OnMouseDoubleClick(object sender, MouseEventArgs e)
		{
			ShowOwnerWindow();
		}

		private void ShowNotifyWindow()
		{
			var window = new NotifyWindow(_ownerWindow, GetClickedPoint());
			window.Show();
		}

		private void ShowOwnerWindow()
		{
			if (_ownerWindow.WindowState == WindowState.Minimized)
			{
				_ownerWindow.WindowState = WindowState.Normal;

				_ownerWindow.Show();
				_ownerWindow.Activate();
			}
			else
			{
				_ownerWindow.WindowState = WindowState.Minimized;
			}
		}

		/// <summary>
		/// Gets the point where NotifyIcon is clicked by the position of ContextMenuStrip and NotifyIcon.
		/// </summary>
		/// <returns>Cursor location</returns>
		/// <remarks>MouseEventArgs.Location property of MouseClick event does not contain data.</remarks>
		private Point GetClickedPoint()
		{
			var contextMenuStrip = _notifyIcon.ContextMenuStrip;

			var corners = new Point[]
			{
				//new Point(contextMenuStrip.Left, contextMenuStrip.Top),
				//new Point(contextMenuStrip.Right, contextMenuStrip.Top),
				new Point(contextMenuStrip.Left, contextMenuStrip.Bottom),
				new Point(contextMenuStrip.Right, contextMenuStrip.Bottom)
			};

			var notifyIconRect = WindowPosition.GetNotifyIconRect(_notifyIcon);
			if (notifyIconRect != Rect.Empty)
			{
				foreach (var corner in corners)
				{
					if (notifyIconRect.Contains(corner))
						return corner;
				}
			}

			return corners.Last(); // Fallback
		}

		#endregion
	}
}