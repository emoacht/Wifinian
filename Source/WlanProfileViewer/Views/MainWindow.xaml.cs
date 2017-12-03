using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Reactive.Bindings.Extensions;

using ScreenFrame.Movers;
using WlanProfileViewer.Models;
using WlanProfileViewer.ViewModels;

namespace WlanProfileViewer.Views
{
	public partial class MainWindow : Window
	{
		private readonly SwitchWindowMover _mover;
		private MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

		protected CompositeDisposable Subscription { get; } = new CompositeDisposable();

		internal MainWindow(MainController controller)
		{
			InitializeComponent();

			this.ShowInTaskbar = false;

			ThemeService.AdjustResourceColors(Application.Current.Resources);

			this.DataContext = new MainWindowViewModel(controller);

			_mover = new SwitchWindowMover(this, controller.NotifyIconContainer.NotifyIcon);

			#region Height

			this.Height = Settings.Current.MainWindowHeight * _mover.Dpi.DpiScaleY;

			Observable.FromEventPattern<SizeChangedEventHandler, SizeChangedEventArgs>(
				h => h.Invoke,
				h => this.SizeChanged += h,
				h => this.SizeChanged -= h)
				.Where(x => x.EventArgs.HeightChanged)
				.Subscribe(x => Settings.Current.MainWindowHeight = x.EventArgs.NewSize.Height / _mover.Dpi.DpiScaleY)
				.AddTo(this.Subscription);

			#endregion

			#region Drag

			Observable.FromEventPattern<MouseButtonEventHandler, MouseButtonEventArgs>(
				h => h.Invoke,
				h => this.MouseLeftButtonDown += h,
				h => this.MouseLeftButtonDown -= h)
				.Subscribe(x =>
				{
					this.DragMove();
					x.EventArgs.Handled = true;
				})
				.AddTo(this.Subscription);

			#endregion
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			WindowEffect.EnableBackgroundBlur(this);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				Subscription.Dispose();
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