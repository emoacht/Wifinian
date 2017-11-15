using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using WlanProfileViewer.Models;
using WlanProfileViewer.Views;

namespace WlanProfileViewer
{
	public partial class App : Application
	{
		public App()
		{
			if (!Debugger.IsAttached)
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (ProcessService.CheckActivateExistingProcess())
			{
				Application.Current.Shutdown(1); // This exit code is for unusual shutdown.
				return;
			}

			if (!Debugger.IsAttached)
				this.DispatcherUnhandledException += OnDispatcherUnhandledException;

			Settings.Current.Initialize();

			this.MainWindow = new MainWindow();
			this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			Settings.Current.Dispose();

			base.OnExit(e);
		}

		#region Exception

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			LogService.RecordException(sender, e.ExceptionObject as Exception);
		}

		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			LogService.RecordException(sender, e.Exception);

			e.Handled = true;
			Application.Current.Shutdown(1); // This exit code is for unusual shutdown.
		}

		#endregion
	}
}