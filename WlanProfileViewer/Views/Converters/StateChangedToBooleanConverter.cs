using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Reactive.Bindings.Interactivity;

namespace WlanProfileViewer.Views.Converters
{
	/// <summary>
	/// Convert StateChanged event to Boolean for EventToReactiveProperty.
	/// </summary>
	public class StateChangedToBooleanConverter : ReactiveConverter<EventArgs, bool>
	{
		protected override IObservable<bool> OnConvert(IObservable<EventArgs> source)
		{
			var window = this.AssociateObject as Window;

			return source.Select(_ => window?.WindowState == WindowState.Minimized);
		}
	}
}