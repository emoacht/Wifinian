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

		public ReactiveProperty<bool> RushesRescan => _controller.RushesRescan;
		public ReactiveProperty<bool> EngagesPriority => _controller.EngagesPriority;
		public ReactiveProperty<bool> ReordersPriority { get; }

		public ReadOnlyReactiveProperty<bool> IsUpdating { get; }
		public ReadOnlyReactiveProperty<bool> CanDelete { get; }

		public ReactiveCommand RescanCommand => _controller.RescanCommand;
		public ReactiveCommand MoveUpCommand { get; }
		public ReactiveCommand MoveDownCommand { get; }
		public ReactiveCommand DeleteCommand { get; }

		public MainWindowViewModel() : this(new MainController())
		{ }

		internal MainWindowViewModel(MainController controller)
		{
			this._controller = controller;

			Profiles = _controller.Profiles
				.ToReadOnlyReactiveCollection(x => new ProfileItemViewModel(_controller, x))
				.AddTo(this.Subscription);
			Profiles
				.ObserveElementObservableProperty(x => x.Position)
				.Throttle(TimeSpan.FromMilliseconds(10))
				.ObserveOn(SynchronizationContext.Current)
				.Subscribe(_ => ProfilesView.Refresh()) // ListCollectionView.Refresh method seems not thread-safe.
				.AddTo(this.Subscription);

			ReordersPriority = new ReactiveProperty<bool>()
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

			#region Work

			var isNotWorking = _controller.IsWorking
				.Inverse()
				.StartWith(true) // This is necessary for initial query.
				.ObserveOnUIDispatcher() // This is safety for thread access with ReactiveCommand.
				.Publish();

			var selectedProfile = Profiles
				.ObserveElementObservableProperty(x => x.IsSelected)
				.Where(x => x.Value)
				.Select(x => x.Instance)
				.Publish();

			var connectedProfile = Profiles
				.ObserveElementObservableProperty(x => x.IsConnected)
				.Where(x => x.Instance.IsSelected.Value)
				.Select(x => x.Instance)
				.Publish();

			var canMoveUp = selectedProfile
				.Select(x => x.Position.Value > 0);

			MoveUpCommand = new[] { isNotWorking, canMoveUp }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			MoveUpCommand
				.Subscribe(async _ => await _controller.MoveUpProfileAsync())
				.AddTo(this.Subscription);

			var canMoveDown = selectedProfile
				.Select(x => x.Position.Value < x.PositionCount.Value - 1);

			MoveDownCommand = new[] { isNotWorking, canMoveDown }
				.CombineLatestValuesAreAllTrue()
				.ToReactiveCommand();
			MoveDownCommand
				.Subscribe(async _ => await _controller.MoveDownProfileAsync())
				.AddTo(this.Subscription);

			CanDelete = Observable.Merge(selectedProfile, connectedProfile)
				.Select(x => !x.IsConnected.Value)
				.Merge(isNotWorking)
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			DeleteCommand = CanDelete
				.ToReactiveCommand();
			DeleteCommand
				.Subscribe(async _ => await _controller.DeleteProfileAsync())
				.AddTo(this.Subscription);

			isNotWorking.Connect().AddTo(this.Subscription);
			selectedProfile.Connect().AddTo(this.Subscription);
			connectedProfile.Connect().AddTo(this.Subscription);

			#endregion
		}
	}
}