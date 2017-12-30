using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;

using Wifinian.Common;
using Wifinian.Models;

namespace Wifinian.ViewModels
{
	public class MenuWindowViewModel : DisposableBase
	{
		private readonly MainController _controller;

		public ReactiveCommand CloseCommand => _controller?.CloseCommand;

		internal MenuWindowViewModel(MainController controller)
		{
			this._controller = controller;
		}

		#region Startup

		public bool CanRegister => StartupService.CanRegister();

		public bool IsRegistered
		{
			get
			{
				if (!_isRegistered.HasValue)
				{
					_isRegistered = StartupService.IsRegistered();
				}
				return _isRegistered.Value;
			}
			set
			{
				if (_isRegistered == value)
					return;

				if (value)
				{
					StartupService.Register();
				}
				else
				{
					StartupService.Unregister();
				}
				_isRegistered = value;
				RaisePropertyChanged();
			}
		}
		private bool? _isRegistered;

		#endregion
	}
}