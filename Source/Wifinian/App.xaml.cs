using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using StartupAgency;
using Wifinian.Models;

namespace Wifinian
{
	public partial class App : Application
	{
		private StartupAgent _agent;
		private MainController _controller;

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			LogService.Start();
			
			_agent = new StartupAgent();
			if (!_agent.Start(ProductInfo.StartupTaskId))
			{
				this.Shutdown(0); // This shutdown is expected behavior.
				return;
			}

			_controller = new MainController(_agent);
			await _controller.InitiateAsync();

			//this.MainWindow = new MainWindow();
			//this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_controller?.Dispose();
			_agent?.Dispose();

			LogService.End();

			base.OnExit(e);
		}
	}
}