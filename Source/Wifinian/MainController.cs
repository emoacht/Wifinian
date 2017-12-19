using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;
using Reactive.Bindings.Notifiers;

using ScreenFrame;
using Wifinian.Common;
using Wifinian.Models;
using Wifinian.Models.Wlan;
using Wifinian.Views;

namespace Wifinian
{
	internal class MainController : DisposableBase
	{
		private readonly Application _current = Application.Current;

		public ObservableCollection<ProfileItem> Profiles { get; }
		private readonly object _profilesLock = new object();

		public NotifyIconContainer NotifyIconContainer { get; }

		private readonly IWlanWorker _worker;

		public BooleanNotifier IsUpdating { get; }
		public BooleanNotifier IsWorking { get; }

		public ReactiveProperty<bool> RushesRescan { get; }
		public ReactiveProperty<bool> EngagesPriority { get; }

		private ReactiveTimer RescanTimer { get; }

		public ReactiveCommand RescanCommand { get; }
		public ReactiveCommand CloseCommand { get; }

		public MainController() : this(
			//new MockWorker() ??
			//new NetshWorker() ??
			new NativeWifiWorker())
		{ }

		public MainController(IWlanWorker worker)
		{
			Profiles = new ObservableCollection<ProfileItem>();
			BindingOperations.EnableCollectionSynchronization(Profiles, _profilesLock);

			NotifyIconContainer = new NotifyIconContainer();
			NotifyIconContainer.MouseLeftButtonClick += OnMainWindowShowRequested;
			NotifyIconContainer.MouseRightButtonClick += OnMenuWindowShowRequested;

			this._worker = worker;

			IsUpdating = new BooleanNotifier();
			IsWorking = new BooleanNotifier();

			ShowUpdatingTime(); // For debug
			ShowWorkingTime(); // For debug

			RushesRescan = new ReactiveProperty<bool>()
				.AddTo(this.Subscription);

			EngagesPriority = new ReactiveProperty<bool>()
				.AddTo(this.Subscription);

			#region Update

			RescanTimer = new ReactiveTimer(TimeSpan.FromSeconds(Settings.Current.RescanInterval))
				.AddTo(this.Subscription);

			RescanCommand = IsUpdating
				.Inverse()
				.ToReactiveCommand();
			RescanCommand
				.Merge(EngagesPriority.Where(x => x).Select(x => x as object))
				.Merge(RescanTimer.Select(x => x as object))
				.Subscribe(async _ => await ScanNetworkAsync())
				.AddTo(this.Subscription);

			Settings.Current
				.ObserveProperty(x => x.RescanInterval)
				.Subscribe(rescanInterval => RescanTimer.Interval = TimeSpan.FromSeconds(rescanInterval))
				.AddTo(this.Subscription);

			RushesRescan
				.Subscribe(rushesRescan =>
				{
					if (rushesRescan)
						RescanTimer.Start();
					else
						RescanTimer.Stop();
				})
				.AddTo(this.Subscription);

			var networkRefreshed = Observable.FromEventPattern(
				h => _worker.NetworkRefreshed += h,
				h => _worker.NetworkRefreshed -= h);
			var interfaceChanged = Observable.FromEventPattern(
				h => _worker.InterfaceChanged += h,
				h => _worker.InterfaceChanged -= h);
			var connectionChanged = Observable.FromEventPattern(
				h => _worker.ConnectionChanged += h,
				h => _worker.ConnectionChanged -= h);
			var profileChanged = Observable.FromEventPattern(
				h => _worker.ProfileChanged += h,
				h => _worker.ProfileChanged -= h);
			Observable.Merge(networkRefreshed, interfaceChanged, connectionChanged, profileChanged)
				.Throttle(TimeSpan.FromMilliseconds(100))
				.Subscribe(async _ =>
				{
					if (RushesRescan.Value)
						RescanTimer.Start(TimeSpan.FromSeconds(Settings.Current.RescanInterval)); // Wait for due time.

					await LoadProfilesAsync();
				})
				.AddTo(this.Subscription);

			#endregion

			#region Close

			CloseCommand = new ReactiveProperty<bool>(true)
				.ToReactiveCommand();
			CloseCommand
				.Subscribe(_ => _current.Shutdown())
				.AddTo(this.Subscription);

			#endregion
		}

		public async Task InitiateAsync()
		{
			Settings.Current.Initiate();

			NotifyIconContainer.ShowIcon("pack://application:,,,/Resources/ring.ico", ProductInfo.Title);

			await LoadProfilesAsync();

			_current.MainWindow = new MainWindow(this);
			_current.MainWindow.Show();

			Observable.FromEventPattern(
				h => _current.MainWindow.Activated += h,
				h => _current.MainWindow.Activated -= h)
				.StartWith(new object()) // This is necessary for initial query.
				.Subscribe(async _ => await ScanNetworkAsync())
				.AddTo(this.Subscription);
		}

		#region Dispose

		private bool _disposed = false;

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
			{
				_worker?.Dispose();
				NotifyIconContainer.Dispose();
				Settings.Current.Dispose();
			}

			_disposed = true;

			base.Dispose(disposing);
		}

		#endregion

		private void OnMainWindowShowRequested(object sender, EventArgs e)
		{
			ShowMainWindow();
		}

		private void OnMenuWindowShowRequested(object sender, Point e)
		{
			ShowMenuWindow(e);
		}

		private void ShowMainWindow()
		{
			var window = (MainWindow)_current.MainWindow;
			if (!window.CanBeShown)
				return;

			if (window.Visibility != Visibility.Visible)
			{
				window.Show();
			}
			window.Activate();
		}

		private void ShowMenuWindow(Point pivot)
		{
			var window = new MenuWindow(this, pivot);
			window.Show();
		}

		#region Update

		private static readonly TimeSpan _scanTimeout = TimeSpan.FromSeconds(5);

		private Task ScanNetworkAsync()
		{
			Debug.WriteLine("Scan start!");

			return UpdateAsync(() => _worker.ScanNetworkAsync(_scanTimeout));
		}

		private Task LoadProfilesAsync()
		{
			Debug.WriteLine("Load start!");

			return UpdateAsync(() => Task.Run(() => LoadProfilesBaseAsync()));
		}

		private async Task LoadProfilesBaseAsync()
		{
			var oldProfileIndices = Enumerable.Range(0, Profiles.Count).ToList();
			var newProfiles = new List<ProfileItem>();

			foreach (var newProfile in await _worker.GetProfilesAsync())
			{
				var isExisting = false;

				foreach (int index in oldProfileIndices)
				{
					var oldProfile = Profiles[index];
					if (string.Equals(oldProfile.Id, newProfile.Id, StringComparison.Ordinal))
					{
						isExisting = true;
						oldProfile.Copy(newProfile);
						oldProfileIndices.Remove(index);
						break;
					}
				}

				if (!isExisting)
					newProfiles.Add(newProfile);
			}

			if ((oldProfileIndices.Count > 0) || (newProfiles.Count > 0))
			{
				lock (_profilesLock)
				{
					oldProfileIndices.Reverse(); // Reverse indices to start removing from the tail.
					oldProfileIndices.ForEach(x => Profiles.RemoveAt(x));
					newProfiles.ForEach(x => Profiles.Add(x));
				}

				// Calculate count of positions for each interface.
				Profiles
					.GroupBy(x => x.InterfaceId)
					.ToList()
					.ForEach(profilesGroup =>
					{
						var count = profilesGroup.Count();

						foreach (var profile in profilesGroup)
							profile.PositionCount = count;
					});
			}

			Debug.WriteLine(Profiles.Any()
				? Profiles
					.Select(x => $"Profile {x.Name} -> AutoConnect {x.IsAutoConnectEnabled}, AutoSwitch {x.IsAutoSwitchEnabled}, Position: {x.Position}, Signal: {x.Signal}, IsConnected {x.IsConnected}")
					.Aggregate((work, next) => work + Environment.NewLine + next)
				: "No Profile");

			if (EngagesPriority.Value)
			{
				var targetProfiles = Profiles
					.Where(x => x.IsAutoConnectEnabled && x.IsAutoSwitchEnabled && (Settings.Current.SignalThreshold <= x.Signal))
					.GroupBy(x => x.InterfaceId)
					.Select(y => y.OrderBy(x => x.Position).Where(x => !x.IsConnected).FirstOrDefault())
					.Where(x => x != null)
					.ToArray();

				if (targetProfiles.Length > 0)
				{
					await Task.WhenAll(targetProfiles.Select(x => ConnectNetworkAsync(x, false)));
				}
			}
		}

		private readonly SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1);

		private async Task UpdateAsync(Func<Task> perform)
		{
			bool isEntered = false;
			try
			{
				if (!(isEntered = _updateSemaphore.Wait(TimeSpan.Zero)))
					return;

				IsUpdating.TurnOn();

				await perform.Invoke();
			}
			finally
			{
				if (isEntered)
				{
					_updateSemaphore.Release();
					IsUpdating.TurnOff();
				}
			}
		}

		[Conditional("DEBUG")]
		private void ShowUpdatingTime()
		{
			IsUpdating
				.Select(x => new { Value = x, DateTime.Now.Ticks })
				.Pairwise()
				.Where(x => x.OldItem.Value && !x.NewItem.Value)
				.Select(x => (x.NewItem.Ticks - x.OldItem.Ticks) / TimeSpan.TicksPerMillisecond)
				.Subscribe(x => Debug.WriteLine($"Updating Time: {x}"))
				.AddTo(this.Subscription);
		}

		#endregion

		#region Work

		private static readonly TimeSpan _connectTimeout = TimeSpan.FromSeconds(10);

		public Task<bool> ChangeProfileOptionAsync(ProfileItem targetProfile)
		{
			Debug.WriteLine("ChangeParameter start!");

			return WorkAsync(targetProfile, x => _worker.SetProfileOptionAsync(x));
		}

		public Task<bool> MoveUpProfileAsync()
		{
			Debug.WriteLine("MoveUp start!");

			if (!TryGetTargetProfile(out ProfileItem targetProfile))
				return Task.FromResult(false);

			var oldPosition = targetProfile.Position;
			var newPosition = oldPosition - 1;
			if (newPosition < 0)
				return Task.FromResult(false);

			return WorkAsync(targetProfile, x => _worker.SetProfilePositionAsync(x, newPosition));
		}

		public Task<bool> MoveDownProfileAsync()
		{
			Debug.WriteLine("MoveDown start!");

			if (!TryGetTargetProfile(out ProfileItem targetProfile))
				return Task.FromResult(false);

			var oldPosition = targetProfile.Position;
			var newPosition = oldPosition + 1;
			if (newPosition > targetProfile.PositionCount - 1)
				return Task.FromResult(false);

			return WorkAsync(targetProfile, x => _worker.SetProfilePositionAsync(x, newPosition));
		}

		public Task<bool> DeleteProfileAsync()
		{
			Debug.WriteLine("Delete start!");

			if (!TryGetTargetProfile(out ProfileItem targetProfile))
				return Task.FromResult(false);

			return WorkAsync(targetProfile, x => _worker.DeleteProfileAsync(x));
		}

		public Task<bool> ConnectNetworkAsync(ProfileItem targetProfile, bool isManual = true)
		{
			Debug.WriteLine("Connect start!");

			if (isManual)
				EngagesPriority.Value = false;

			return WorkAsync(targetProfile, x => _worker.ConnectNetworkAsync(x, _connectTimeout));
		}

		public Task<bool> DisconnectNetworkAsync(ProfileItem targetProfile, bool isManual = true)
		{
			Debug.WriteLine("Disconnect start!");

			if (isManual)
				EngagesPriority.Value = false;

			return WorkAsync(targetProfile, x => _worker.DisconnectNetworkAsync(x, _connectTimeout));
		}

		private bool TryGetTargetProfile(out ProfileItem targetProfile) =>
			((targetProfile = Profiles.FirstOrDefault(x => x.IsTarget)) != null);

		private int _workCount = 0;

		private async Task<bool> WorkAsync(ProfileItem targetProfile, Func<ProfileItem, Task<bool>> perform)
		{
			try
			{
				Interlocked.Increment(ref _workCount);
				IsWorking.TurnOn();

				return await perform.Invoke(targetProfile);
			}
			finally
			{
				if (Interlocked.Decrement(ref _workCount) == 0)
					IsWorking.TurnOff();
			}
		}

		[Conditional("DEBUG")]
		private void ShowWorkingTime()
		{
			IsWorking
				.Select(x => new { Value = x, DateTime.Now.Ticks })
				.Pairwise()
				.Where(x => x.OldItem.Value && !x.NewItem.Value)
				.Select(x => (x.NewItem.Ticks - x.OldItem.Ticks) / TimeSpan.TicksPerMillisecond)
				.Subscribe(x => Debug.WriteLine($"Working Time: {x}"))
				.AddTo(this.Subscription);
		}

		#endregion
	}
}