using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;

using WlanProfileViewer.Common;
using WlanProfileViewer.Models;
using WlanProfileViewer.Models.Wlan;

namespace WlanProfileViewer.ViewModels
{
	public class MainWindowViewModel : BindableBase
	{
		private Operation Op { get; }

		public ReadOnlyReactiveCollection<ProfileItemViewModel> Profiles { get; }

		public ListCollectionView ProfilesView
		{
			get
			{
				if (_profilesView == null)
				{
					_profilesView = new ListCollectionView(Profiles);
					_profilesView.SortDescriptions.Add(new SortDescription(nameof(ProfileItemViewModel.InterfaceDescription), ListSortDirection.Ascending));
					_profilesView.SortDescriptions.Add(new SortDescription($"{nameof(ProfileItemViewModel.Position)}.{nameof(IReactiveProperty.Value)}", ListSortDirection.Ascending));
				}

				return _profilesView;
			}
		}
		private ListCollectionView _profilesView;

		public ReactiveProperty<bool> IsAutoReloadEnabled { get; }
		public ReactiveProperty<bool> IsSuspended { get; }
		public ReactiveProperty<bool> IsConfigMode { get; }

		public ReadOnlyReactiveProperty<bool> IsLoading { get; }
		public ReadOnlyReactiveProperty<bool> IsNotWorking { get; }

		public ReactiveCommand ReloadCommand { get; }

		public ReactiveCommand MoveUpCommand { get; }
		public ReactiveCommand MoveDownCommand { get; }
		public ReactiveCommand DeleteCommand { get; }
		public ReactiveCommand ConnectCommand { get; }
		public ReactiveCommand DisconnectCommand { get; }

		public MainWindowViewModel()
		{
			Op = new Operation(
				//new MockWorker() ??
				//new NetshWorker() ??
				new NativeWifiWorker() as IWlanWorker);

			this.Profiles = Op.Profiles.ToReadOnlyReactiveCollection(x => new ProfileItemViewModel(x));

			#region AutoReloadEnabled/Suspended/ConfigMode

			IsAutoReloadEnabled = Op
				.ToReactivePropertyAsSynchronized(x => x.IsAutoReloadEnabled);

			IsSuspended = Op
				.ToReactivePropertyAsSynchronized(x => x.IsSuspended);

			IsConfigMode = new ReactiveProperty<bool>();

			IsAutoReloadEnabled
				.Merge(IsSuspended)
				.Where(x => x)
				.Subscribe(_ => IsConfigMode.Value = false);

			IsConfigMode
				.Where(x => x)
				.Subscribe(_ => IsAutoReloadEnabled.Value = false);

			#endregion

			#region Load

			IsLoading = Op.IsLoading
				.Where(_ => !Op.IsWorking.Value)
				//.Select(x => Observable.Empty<bool>()
				//	.Delay(TimeSpan.FromMilliseconds(10))
				//	.StartWith(x))
				//.Concat()
				.ObserveOnUIDispatcher()
				.ToReadOnlyReactiveProperty();

			ReloadCommand = IsLoading
				.Select(x => !x)
				.ToReactiveCommand();
			ReloadCommand
				.Subscribe(async _ => await Op.LoadProfilesAsync(true));

			Profiles
				.ObserveElementObservableProperty(x => x.Position)
				.Throttle(TimeSpan.FromMilliseconds(10))
				.ObserveOn(SynchronizationContext.Current)
				.Subscribe(_ => ProfilesView.Refresh()); // ListCollectionView.Refresh method seems not thread-safe.

			#endregion

			#region Work

			IsNotWorking = Op.IsWorking
				.Select(x => !x)
				.StartWith(true) // This is necessary for initial query.
				.ObserveOnUIDispatcher()
				.ToReadOnlyReactiveProperty();

			// Query for a profile which is selected.
			var querySelectedProfiles = Profiles
				.ObserveElementObservableProperty(x => x.IsSelected)
				.Where(x => x.Value)
				.Select(x => x.Instance)
				.Publish();

			// Query for the selected profile which is connected or disconnected.
			var queryConnectedProfiles = Profiles
				.ObserveElementObservableProperty(x => x.IsConnected)
				.Where(x => x.Instance.IsSelected.Value)
				.Select(x => x.Instance)
				.Publish();

			// Query for the selected profile which changes to be available or unavailable.
			var queryAvailableProfiles = Profiles
				.ObserveElementObservableProperty(x => x.IsAvailable)
				.Where(x => x.Instance.IsSelected.Value)
				.Select(x => x.Instance)
				.Publish();

			#region MoveUp

			var queryMoveUp = querySelectedProfiles
				.Select(x => x.Position.Value > 0);

			MoveUpCommand = new[] { IsNotWorking, queryMoveUp }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			MoveUpCommand
				.Subscribe(async _ => await Op.MoveUpProfileAsync());

			#endregion

			#region MoveDown

			var queryMoveDown = querySelectedProfiles
				.Select(x => x.Position.Value < x.PositionCount.Value - 1);

			MoveDownCommand = new[] { IsNotWorking, queryMoveDown }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			MoveDownCommand
				.Subscribe(async _ => await Op.MoveDownProfileAsync());

			#endregion

			#region Delete

			DeleteCommand = IsNotWorking
				.ToReactiveCommand();
			DeleteCommand
				.Subscribe(async _ => await Op.DeleteProfileAsync());

			#endregion

			#region Connect

			var queryConnect = Observable.Merge(querySelectedProfiles, queryConnectedProfiles, queryAvailableProfiles)
				.Select(x => !x.IsConnected.Value && x.IsAvailable.Value);

			ConnectCommand = new[] { IsNotWorking, queryConnect }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			ConnectCommand
				.Subscribe(async _ => await Op.ConnectNetworkAsync());

			#endregion

			#region Disconnect

			var queryDisconnect = Observable.Merge(querySelectedProfiles, queryConnectedProfiles)
				.Select(x => x.IsConnected.Value);

			DisconnectCommand = new[] { IsNotWorking, queryDisconnect }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			DisconnectCommand
				.Subscribe(async _ => await Op.DisconnectNetworkAsync());

			#endregion

			querySelectedProfiles.Connect();
			queryConnectedProfiles.Connect();
			queryAvailableProfiles.Connect();

			#endregion
		}
	}
}