using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;

using WlanProfileViewer.Common;
using WlanProfileViewer.Models.Wlan;

namespace WlanProfileViewer.ViewModels
{
	public class ProfileItemViewModel : DisposableBase
	{
		public string Name { get; }
		public string InterfaceDescription { get; }
		public string Authentication { get; }
		public string Encryption { get; }

		public ReactiveProperty<bool> IsAutoConnectEnabled { get; }
		public ReactiveProperty<bool> IsAutoSwitchEnabled { get; }

		public ReadOnlyReactiveProperty<int> Position { get; }
		public ReadOnlyReactiveProperty<int> PositionCount { get; }

		public ReadOnlyReactiveProperty<int> Signal { get; }
		public ReadOnlyReactiveProperty<bool> IsAvailable { get; }
		public ReadOnlyReactiveProperty<bool> IsConnected { get; }

		public ReactiveProperty<bool> IsSelected { get; }

		public ProfileItemViewModel(ProfileItem profileItem)
		{
			Name = profileItem.Name;
			InterfaceDescription = profileItem.InterfaceDescription;
			Authentication = profileItem.Authentication.ToString().Replace("_", "-");
			Encryption = profileItem.Encryption.ToString();

			IsAutoConnectEnabled = profileItem
				.ObserveProperty(x => x.IsAutoConnectEnabled)
				.ToReactiveProperty()
				.AddTo(this.Subscription);

			IsAutoSwitchEnabled = profileItem
				.ObserveProperty(x => x.IsAutoSwitchEnabled)
				.ToReactiveProperty()
				.AddTo(this.Subscription);

			Position = profileItem
				.ObserveProperty(x => x.Position)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			PositionCount = profileItem
				.ObserveProperty(x => x.PositionCount)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			Signal = profileItem
				.ObserveProperty(x => x.Signal)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			IsAvailable = Signal
				.Select(x => 0 < x)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			IsConnected = profileItem
				.ObserveProperty(x => x.IsConnected)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			IsSelected = ReactiveProperty.FromObject(profileItem, x => x.IsTarget)
				.AddTo(this.Subscription);
		}
	}
}