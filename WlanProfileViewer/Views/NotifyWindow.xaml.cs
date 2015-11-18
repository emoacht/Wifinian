using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using WlanProfileViewer.ViewModels;

namespace WlanProfileViewer.Views
{
	public partial class NotifyWindow : Window
	{
		private readonly Window _ownerWindow;
		private readonly Point _pivotLocation;
		private readonly TaskbarAlignment _taskbarAlignment;

		public NotifyWindow() : this(null, default(Point))
		{ }

		public NotifyWindow(Window ownerWindow, Point pivotLocation)
		{
			InitializeComponent();

			this.Topmost = true;
			this.ShowInTaskbar = false;

			// Assigning ownerWindow to this.Owner will bring the owner window at the top (not the topmost)
			// when this window is activated. Such behavior is not desirable.
			this._ownerWindow = ownerWindow;

			if (this._ownerWindow != null)
				this._ownerWindow.Closing += OnOwnerWindowClosing;

			this._pivotLocation = pivotLocation;
			this._taskbarAlignment = WindowPosition.GetTaskbarAlignment();

			this.DataContext = new NotifyWindowViewModel(ownerWindow);
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			SetWindowLocation(arrangeBounds);

			return base.ArrangeOverride(arrangeBounds);
		}

		private void SetWindowLocation(Size windowSize)
		{
			var left = _pivotLocation.X;
			var top = _pivotLocation.Y;

			switch (_taskbarAlignment)
			{
				case TaskbarAlignment.Left:
					// Place this window at the top-right of the pivot.
					left += 1;
					top += -windowSize.Height - 1;
					break;

				case TaskbarAlignment.Top:
					// Place this window at the bottom-left of the pivot.
					left += -windowSize.Width - 1;
					top += 1;
					break;

				case TaskbarAlignment.Right:
				case TaskbarAlignment.Bottom:
					// Place this window at the top-left of the pivot.
					left += -windowSize.Width - 1;
					top += -windowSize.Height - 1;
					break;

				default:
					return;
			}

			if ((this.Left == left) && (this.Top == top))
				return;

			WindowPosition.SetWindowLocation(this, new Point(left, top));
		}

		#region Close

		private bool _isClosing = false;

		protected override void OnClosing(CancelEventArgs e)
		{
			_isClosing = true;

			if (_ownerWindow != null)
				_ownerWindow.Closing -= OnOwnerWindowClosing;

			base.OnClosing(e);
		}

		private void OnOwnerWindowClosing(object sender, CancelEventArgs e)
		{
			if (!_isClosing)
				this.Close();
		}

		protected override void OnDeactivated(EventArgs e)
		{
			if (!_isClosing)
				this.Close();

			//base.OnDeactivated(e);
		}

		#endregion
	}
}