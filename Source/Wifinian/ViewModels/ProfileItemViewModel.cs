using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;

using Wifinian.Common;
using Wifinian.Models.Wlan;

namespace Wifinian.ViewModels
{
	public class ProfileItemViewModel : DisposableBase
	{
		private readonly MainController _controller;

		public string Name
		{
			get => _name;
			set
			{
				if ((_name != null) && !_controller.IsUsableProfileName(InterfaceId, value))
					return;

				SetPropertyValue(ref _name, value);
			}
		}
		private string _name;

		private Guid InterfaceId { get; }
		public string InterfaceDescription { get; }
		public string Authentication { get; }
		public string Encryption { get; }
		public bool CanSetOptions { get; }

		public ReactiveProperty<bool> IsAutoConnectEnabled { get; }
		public ReactiveProperty<bool> IsAutoSwitchEnabled { get; }

		public ReadOnlyReactiveProperty<int> Position { get; }
		public ReadOnlyReactiveProperty<int> PositionCount { get; }

		public ReadOnlyReactiveProperty<bool> IsRadioOn { get; }
		public ReadOnlyReactiveProperty<int> Signal { get; }
		public ReadOnlyReactiveProperty<bool> IsAvailable { get; }
		public ReadOnlyReactiveProperty<bool> IsConnected { get; }

		public ReactiveProperty<bool> IsSelected { get; }

		public ReactiveCommand ConnectCommand { get; }
		public ReactiveCommand DisconnectCommand { get; }

		internal ProfileItemViewModel(MainController controller, ProfileItem profileItem)
		{
			this._controller = controller;

			Name = profileItem.Name;
			InterfaceId = profileItem.InterfaceId;
			InterfaceDescription = profileItem.InterfaceDescription;
			Authentication = profileItem.Authentication.ToString().Replace("_", "-");
			Encryption = profileItem.Encryption.ToString();
			CanSetOptions = profileItem.CanSetOptions;

			this.ObserveProperty(x => x.Name, false)
				.Subscribe(async x => await _controller.RenameProfileAsync(profileItem, x))
				.AddTo(this.Subscription);

			IsAutoSwitchEnabled = profileItem
				.ToReactivePropertyAsSynchronized(x => x.IsAutoSwitchEnabled, ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(this.Subscription);
			IsAutoConnectEnabled = profileItem
				.ToReactivePropertyAsSynchronized(x => x.IsAutoConnectEnabled, ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(this.Subscription);
			IsAutoConnectEnabled
				.Where(x => !x)
				.Subscribe(_ => IsAutoSwitchEnabled.Value = false)
				.AddTo(this.Subscription);

			Observable.Merge(IsAutoConnectEnabled, IsAutoSwitchEnabled)
				.Subscribe(async _ => await _controller.ChangeProfileOptionAsync(profileItem))
				.AddTo(this.Subscription);

			Position = profileItem
				.ObserveProperty(x => x.Position)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			PositionCount = profileItem
				.ObserveProperty(x => x.PositionCount)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			IsRadioOn = profileItem
				.ObserveProperty(x => x.IsRadioOn)
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

			#region Work

			var isNotWorking = _controller.IsWorking
				.Inverse()
				.StartWith(true) // This is necessary for initial query.
				.ObserveOnUIDispatcher() // This is safety for thread access with ReactiveCommand.
				.Publish();

			ConnectCommand = new[] { isNotWorking, IsAvailable, IsConnected.Inverse() }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			ConnectCommand
				.Subscribe(async _ => await _controller.ConnectNetworkAsync(profileItem))
				.AddTo(this.Subscription);

			DisconnectCommand = new[] { isNotWorking.AsObservable(), IsConnected }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			DisconnectCommand
				.Subscribe(async _ => await _controller.DisconnectNetworkAsync(profileItem))
				.AddTo(this.Subscription);

			isNotWorking.Connect().AddTo(this.Subscription);

			#endregion
		}
	}
}