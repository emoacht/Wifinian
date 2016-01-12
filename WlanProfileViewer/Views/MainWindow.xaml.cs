using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using MonitorAware.Models;
using WlanProfileViewer.Models;

namespace WlanProfileViewer.Views
{
	public partial class MainWindow : Window
	{
		#region Property

		public static string ProductTitle { get; } =
			((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute))).Title;

		public static Version ProductVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version;

		#endregion

		public MainWindow()
		{
			InitializeComponent();

			ThemeService.AdjustResourceColors(Application.Current.Resources);
		}

		private NotifyIconComponent _component;

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			new WindowPlacement().Load(this);

			var notificationAreaDpi = DpiChecker.GetNotificationAreaDpi();
			_component = new NotifyIconComponent(this);
			_component.ShowIcon("pack://application:,,,/Resources/ring.ico", ProductTitle, notificationAreaDpi.X);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (e.Cancel)
				return;

			new WindowPlacement().Save(this);

			_component.Dispose();
		}

		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);

			this.ShowInTaskbar = (this.WindowState != WindowState.Minimized);
		}
	}
}