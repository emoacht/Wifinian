using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Wifinian.Models;

namespace Wifinian
{
	public partial class App : Application
	{
		public App()
		{
			if (!Debugger.IsAttached)
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		}

		private MainController _controller;

		protected async override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (ProcessService.ActivateExistingProcess())
			{
				this.Shutdown(1); // This exit code is for unusual shutdown.
				return;
			}

			if (!Debugger.IsAttached)
				this.DispatcherUnhandledException += OnDispatcherUnhandledException;

			_controller = new MainController();
			await _controller.InitiateAsync();

			//this.MainWindow = new MainWindow();
			//this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_controller?.Dispose();

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
			this.Shutdown(1); // This exit code is for unusual shutdown.
		}

		#endregion
	}
}