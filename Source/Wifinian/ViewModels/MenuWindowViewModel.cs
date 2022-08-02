using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;

using Wifinian.Common;

namespace Wifinian.ViewModels
{
	public class MenuWindowViewModel : DisposableBase
	{
		private readonly AppController _controller;

		public ReactiveCommand CloseCommand => _controller?.CloseCommand;

		internal MenuWindowViewModel(AppController controller)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));
		}

		#region Startup

		public bool CanRegister => _controller.StartupAgent.CanRegister();

		public bool IsRegistered
		{
			get => _isRegistered ??= _controller.StartupAgent.IsRegistered();
			set
			{
				if (_isRegistered == value)
					return;

				if (value)
				{
					_controller.StartupAgent.Register();
				}
				else
				{
					_controller.StartupAgent.Unregister();
				}
				_isRegistered = value;
				OnPropertyChanged();
			}
		}
		private bool? _isRegistered;

		#endregion
	}
}