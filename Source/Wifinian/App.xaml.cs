using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using Wifinian.Models;

namespace Wifinian
{
	public partial class App : Application
	{
		private MainController _controller;

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			LogService.Start();

			if (ProcessService.ActivateExistingProcess())
			{
				this.Shutdown(0); // This shutdown is expected behavior.
				return;
			}

			_controller = new MainController();
			await _controller.InitiateAsync();

			//this.MainWindow = new MainWindow();
			//this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_controller?.Dispose();

			LogService.End();

			base.OnExit(e);
		}
	}
}