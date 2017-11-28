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

using WlanProfileViewer.Common;

namespace WlanProfileViewer.ViewModels
{
	public class MenuWindowViewModel : DisposableBase
	{
		private readonly MainController _controller;

		public ReactiveCommand CloseCommand => _controller?.CloseCommand;

		internal MenuWindowViewModel(MainController controller)
		{
			this._controller = controller;
		}
	}
}