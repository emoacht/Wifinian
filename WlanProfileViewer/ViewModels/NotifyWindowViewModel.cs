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
using WlanProfileViewer.Views;

namespace WlanProfileViewer.ViewModels
{
	public class NotifyWindowViewModel : BindableDisposableBase
	{
		private readonly MainWindow _mainWindow;

		private ReadOnlyReactiveCollection<ProfileItemViewModel> Profiles { get; }

		public ListCollectionView ProfilesView
		{
			get
			{
				if (_profilesView == null)
				{
					_profilesView = new ListCollectionView(Profiles);
					_profilesView.Filter = x => ((ProfileItemViewModel)x).IsConnected.Value;
					_profilesView.SortDescriptions.Add(new SortDescription(nameof(ProfileItemViewModel.InterfaceDescription), ListSortDirection.Ascending));
				}

				return _profilesView;
			}
		}
		private ListCollectionView _profilesView;

		public ReadOnlyReactiveProperty<bool> IsAnyConnected { get; }

		public NotifyWindowViewModel(Window ownerWindow)
		{
			this._mainWindow = ownerWindow as MainWindow;
			if (this._mainWindow == null)
				throw new ArgumentException(nameof(ownerWindow));

			var mainWindowViewModel = this._mainWindow.DataContext as MainWindowViewModel;

			this.Profiles = mainWindowViewModel.Profiles;

			IsAnyConnected = this.Profiles
				.ObserveElementObservableProperty(x => x.IsConnected)
				.ObserveOnUIDispatcher()
				.Select(_ =>
				{
					this.ProfilesView.Refresh();
					return (this.ProfilesView.Count > 0);
				})
				.ToReadOnlyReactiveProperty()
				.AddTo(this.Subscription);

			mainWindowViewModel.ReloadCommand.Execute();
		}
	}
}