using System;
using System.Collections.Generic;
using System.ComponentModel;
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

using ScreenFrame.Movers;
using WlanProfileViewer.ViewModels;

namespace WlanProfileViewer.Views
{
	public partial class MainWindow : Window
	{
		private readonly SwitchWindowMover _mover;
		private MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

		internal MainWindow(MainController controller)
		{
			InitializeComponent();

			this.ShowInTaskbar = false;

			ThemeService.AdjustResourceColors(Application.Current.Resources);

			this.DataContext = new MainWindowViewModel(controller);

			_mover = new SwitchWindowMover(this, controller.NotifyIconContainer.NotifyIcon);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				ViewModel.Dispose();
			}

			base.OnClosing(e);
		}

		#region Show/Hide

		public bool CanBeShown => (_preventionTime < DateTimeOffset.Now);
		private DateTimeOffset _preventionTime;

		protected override void OnDeactivated(EventArgs e)
		{
			base.OnDeactivated(e);

			if (_mover.IsDeparted)
				return;

			if (this.Visibility != Visibility.Visible)
				return;

			// Set time to prevent this window from being shown unintentionally. 
			_preventionTime = DateTimeOffset.Now + TimeSpan.FromSeconds(0.2);

			// Clear focus.
			FocusManager.SetFocusedElement(this, null);

			this.Hide();
		}

		#endregion
	}
}