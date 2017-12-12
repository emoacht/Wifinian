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

using Wifinian.Common;

namespace Wifinian.ViewModels
{
	public class MainWindowViewModel : DisposableBase
	{
		private readonly MainController _controller;

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

		public ReactiveProperty<bool> IsQuickRescanEnabled => _controller.IsQuickRescanEnabled;
		public ReactiveProperty<bool> IsActivePriorityEnabled => _controller.IsActivePriorityEnabled;
		public ReactiveProperty<bool> IsConfigMode { get; }

		public ReadOnlyReactiveProperty<bool> IsUpdating { get; }
		public ReadOnlyReactiveProperty<bool> IsNotWorking { get; }

		public ReactiveCommand RescanCommand => _controller.RescanCommand;
		public ReactiveCommand MoveUpCommand { get; }
		public ReactiveCommand MoveDownCommand { get; }
		public ReactiveCommand DeleteCommand { get; }
		public ReactiveCommand ConnectCommand { get; }
		public ReactiveCommand DisconnectCommand { get; }

		public MainWindowViewModel() : this(new MainController())
		{ }

		internal MainWindowViewModel(MainController controller)
		{
			this._controller = controller;

			Profiles = _controller.Profiles
				.ToReadOnlyReactiveCollection(x => new ProfileItemViewModel(x))
				.AddTo(this.Subscription);
			Profiles
				.ObserveElementObservableProperty(x => x.Position)
				.Throttle(TimeSpan.FromMilliseconds(10))
				.ObserveOn(SynchronizationContext.Current)
				.Subscribe(_ => ProfilesView.Refresh()) // ListCollectionView.Refresh method seems not thread-safe.
				.AddTo(this.Subscription);

			IsConfigMode = new ReactiveProperty<bool>()
				.AddTo(this.Subscription);

			IsUpdating = _controller.IsUpdating
				.Where(_ => !_controller.IsWorking.Value)
				//.Select(x => Observable.Empty<bool>()
				//	.Delay(TimeSpan.FromMilliseconds(10))
				//	.StartWith(x))
				//.Concat()
				.ObserveOnUIDispatcher()
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			IsNotWorking = _controller.IsWorking
				.Select(x => !x)
				.StartWith(true) // This is necessary for initial query.
				.ObserveOnUIDispatcher()
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			#region Work

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
				.Subscribe(async _ => await _controller.MoveUpProfileAsync())
				.AddTo(this.Subscription);

			#endregion

			#region MoveDown

			var queryMoveDown = querySelectedProfiles
				.Select(x => x.Position.Value < x.PositionCount.Value - 1);

			MoveDownCommand = new[] { IsNotWorking, queryMoveDown }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			MoveDownCommand
				.Subscribe(async _ => await _controller.MoveDownProfileAsync())
				.AddTo(this.Subscription);

			#endregion

			#region Delete

			DeleteCommand = IsNotWorking
				.ToReactiveCommand();
			DeleteCommand
				.Subscribe(async _ => await _controller.DeleteProfileAsync())
				.AddTo(this.Subscription);

			#endregion

			#region Connect

			var queryConnect = Observable.Merge(querySelectedProfiles, queryConnectedProfiles, queryAvailableProfiles)
				.Select(x => !x.IsConnected.Value && x.IsAvailable.Value);

			ConnectCommand = new[] { IsNotWorking, queryConnect }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			ConnectCommand
				.Subscribe(async _ => await _controller.ConnectNetworkAsync())
				.AddTo(this.Subscription);

			#endregion

			#region Disconnect

			var queryDisconnect = Observable.Merge(querySelectedProfiles, queryConnectedProfiles)
				.Select(x => x.IsConnected.Value);

			DisconnectCommand = new[] { IsNotWorking, queryDisconnect }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			DisconnectCommand
				.Subscribe(async _ => await _controller.DisconnectNetworkAsync())
				.AddTo(this.Subscription);

			#endregion

			querySelectedProfiles.Connect().AddTo(this.Subscription);
			queryConnectedProfiles.Connect().AddTo(this.Subscription);
			queryAvailableProfiles.Connect().AddTo(this.Subscription);

			#endregion
		}
	}
}