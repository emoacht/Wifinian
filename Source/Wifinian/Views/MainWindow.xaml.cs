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

using ScreenFrame;
using ScreenFrame.Movers;
using Wifinian.Helper;
using Wifinian.Models;
using Wifinian.ViewModels;

namespace Wifinian.Views
{
	public partial class MainWindow : Window
	{
		private readonly SwitchWindowMover _mover;
		private MainWindowViewModel ViewModel => (MainWindowViewModel)this.DataContext;

		protected CompositeDisposable Subscription { get; } = new CompositeDisposable();

		internal MainWindow(MainController controller)
		{
			InitializeComponent();

			ThemeService.AdjustResourceColors(Application.Current.Resources);

			this.DataContext = new MainWindowViewModel(controller);

			_mover = new SwitchWindowMover(this, controller.NotifyIconContainer.NotifyIcon);

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

			#region Size

			// This restoration must be done here because the DPI value is incorrect at constructor.
			RestoreWindowSize();

			Observable.FromEventPattern<SizeChangedEventHandler, SizeChangedEventArgs>(
				h => h.Invoke,
				h => this.SizeChanged += h,
				h => this.SizeChanged -= h)
				.Subscribe(x => SaveWindowSize(x.EventArgs.NewSize))
				.AddTo(this.Subscription);

			Observable.FromEventPattern(
				h => _mover.IsDepartedChanged += h,
				h => _mover.IsDepartedChanged -= h)
				.Subscribe(_ => SetResizeBorderThickness())
				.AddTo(this.Subscription);

			#endregion
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

		#region Size

		public Thickness ResizeBorderThickness
		{
			get { return (Thickness)GetValue(ResizeBorderThicknessProperty); }
			set { SetValue(ResizeBorderThicknessProperty, value); }
		}
		public static readonly DependencyProperty ResizeBorderThicknessProperty =
			DependencyProperty.Register(
				"ResizeBorderThickness",
				typeof(Thickness),
				typeof(MainWindow),
				new PropertyMetadata(default(Thickness)));

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			SetResizeBorderThickness();
		}

		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);

			if (this.WindowState == WindowState.Maximized)
				this.WindowState = WindowState.Normal;
		}

		private void RestoreWindowSize()
		{
			var windowSize = Settings.Current.MainWindowSize;
			if ((windowSize.Width < this.MinWidth) ||
				(windowSize.Height < this.MinHeight))
				return;

			this.Width = OsVersion.Is10Threshold1OrNewer
				? windowSize.Width
				: windowSize.Width * _mover.Dpi.DpiScaleX;
			this.Height = OsVersion.Is10Threshold1OrNewer
				? windowSize.Height
				: windowSize.Height * _mover.Dpi.DpiScaleY;
		}

		private void SaveWindowSize(Size windowSize)
		{
			Settings.Current.MainWindowSize = OsVersion.Is10Threshold1OrNewer
				? windowSize
				: new Size(
					windowSize.Width / _mover.Dpi.DpiScaleX,
					windowSize.Height / _mover.Dpi.DpiScaleY);
		}

		private void SetResizeBorderThickness()
		{
			var borderWidth = 4D * _mover.Dpi.DpiScaleX;

			switch (_mover.PivotAlignment)
			{
				case PivotAlignment.TopLeft:
					ResizeBorderThickness = new Thickness(0, 0, borderWidth, borderWidth);
					break;
				case PivotAlignment.TopRight:
					ResizeBorderThickness = new Thickness(borderWidth, 0, 0, borderWidth);
					break;
				case PivotAlignment.BottomRight:
					ResizeBorderThickness = new Thickness(borderWidth, borderWidth, 0, 0);
					break;
				case PivotAlignment.BottomLeft:
					ResizeBorderThickness = new Thickness(0, borderWidth, borderWidth, 0);
					break;
				default:
					ResizeBorderThickness = new Thickness(borderWidth);
					break;
			}
		}

		#endregion

		#region Show/Hide

		public bool CanBeShown => (_preventionTime < DateTimeOffset.Now);
		private DateTimeOffset _preventionTime;

		protected override async void OnDeactivated(EventArgs e)
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

			// Wait for this window to be refreshed before being hidden.
			await Task.Delay(TimeSpan.FromSeconds(0.1));

			this.Hide();
		}

		#endregion
	}
}