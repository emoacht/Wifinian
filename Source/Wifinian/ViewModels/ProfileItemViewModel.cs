﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using Wifinian.Common;
using Wifinian.Models.Wlan;

namespace Wifinian.ViewModels
{
	public class ProfileItemViewModel : DisposableBase
	{
		private readonly AppController _controller;

		public string Name
		{
			get => _name;
			set
			{
				if ((_name is not null) && !_controller.IsUsableProfileName(InterfaceId, value))
					return;

				SetProperty(ref _name, value);
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

		public ReadOnlyReactivePropertySlim<int> Position { get; }
		public ReadOnlyReactivePropertySlim<int> PositionCount { get; }

		public ReadOnlyReactivePropertySlim<bool> IsRadioOn { get; }
		public ReadOnlyReactivePropertySlim<bool> IsConnected { get; }

		public ReadOnlyReactivePropertySlim<string> Protocol { get; }
		public ReadOnlyReactivePropertySlim<int> Signal { get; }
		public ReadOnlyReactivePropertySlim<float> Band { get; }
		public ReadOnlyReactivePropertySlim<int> Channel { get; }

		public ReadOnlyReactivePropertySlim<bool> IsAvailable { get; }

		public ReactiveProperty<bool> IsSelected { get; }

		public ReactiveCommand ConnectCommand { get; }
		public ReactiveCommand DisconnectCommand { get; }

		internal ProfileItemViewModel(AppController controller, ProfileItem profileItem)
		{
			this._controller = controller ?? throw new ArgumentNullException(nameof(controller));

			Name = profileItem.Name;
			InterfaceId = profileItem.InterfaceId;
			InterfaceDescription = profileItem.InterfaceDescription;
			Authentication = profileItem.AuthenticationString.Replace("_", "-");
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
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			PositionCount = profileItem
				.ObserveProperty(x => x.PositionCount)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			IsRadioOn = profileItem
				.ObserveProperty(x => x.IsRadioOn)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			IsConnected = profileItem
				.ObserveProperty(x => x.IsConnected)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			Protocol = profileItem
				.ObserveProperty(x => x.Protocol)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			Signal = profileItem
				.ObserveProperty(x => x.Signal)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			Band = profileItem
				.ObserveProperty(x => x.Band)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			Channel = profileItem
				.ObserveProperty(x => x.Channel)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			IsAvailable = Signal
				.Select(x => 0 < x)
				.ToReadOnlyReactivePropertySlim()
				.AddTo(this.Subscription);

			IsSelected = ReactiveProperty.FromObject(profileItem, x => x.IsTarget)
				.AddTo(this.Subscription);

			#region Work

			var isNotWorking = _controller.IsWorking
				.Inverse()
				.StartWith(true) // This is necessary to start combined sequence.
				.Publish();

			ConnectCommand = new[] { isNotWorking, IsAvailable, IsConnected.Inverse() }
				.CombineLatestValuesAreAllTrue()
				.ObserveOnUIDispatcher() // This is for thread access by ReactiveCommand.
				.ToReactiveCommand();
			ConnectCommand
				.Subscribe(async _ => await _controller.ConnectNetworkAsync(profileItem))
				.AddTo(this.Subscription);

			DisconnectCommand = new[] { isNotWorking.AsObservable(), IsConnected }
				.CombineLatestValuesAreAllTrue()
				.ObserveOnUIDispatcher() // This is for thread access by ReactiveCommand.
				.ToReactiveCommand();
			DisconnectCommand
				.Subscribe(async _ => await _controller.DisconnectNetworkAsync(profileItem))
				.AddTo(this.Subscription);

			isNotWorking.Connect().AddTo(this.Subscription);

			#endregion
		}
	}
}