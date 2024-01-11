using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using ManagedNativeWifi;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

using ScreenFrame;
using StartupAgency;
using Wifinian.Common;
using Wifinian.Models;
using Wifinian.Models.Wlan;
using Wifinian.Views;

namespace Wifinian;

internal class AppController : DisposableBase
{
	private readonly Application _current = Application.Current;

	private readonly AppKeeper _keeper;
	internal StartupAgent StartupAgent => _keeper.StartupAgent;

	public ObservableCollection<ProfileItem> Profiles { get; }
	private readonly object _profilesLock = new object();

	public NotifyIconContainer NotifyIconContainer { get; }
	public WindowPainter WindowPainter { get; }

	private readonly IWlanWorker _worker;
	public bool IsWorkable => _worker.IsWorkable;

	public BooleanNotifier IsUpdating { get; }
	public BooleanNotifier IsWorking { get; }

	public ReactiveProperty<bool> RushesRescan { get; }
	public ReactiveProperty<bool> EngagesPriority { get; }

	private ReactiveTimer RescanTimer { get; }

	public ReactiveCommand RescanCommand { get; }
	public ReactiveCommand CloseCommand { get; }

	public AppController(AppKeeper keeper) : this(keeper,
		//new MockWorker() ??
		//new NetshWorker() ??
		(IWlanWorker)new NativeWifiWorker())
	{ }

	public AppController(AppKeeper keeper, IWlanWorker worker)
	{
		this._keeper = keeper ?? throw new ArgumentNullException(nameof(keeper));

		Profiles = new ObservableCollection<ProfileItem>();
		BindingOperations.EnableCollectionSynchronization(Profiles, _profilesLock);

		NotifyIconContainer = new NotifyIconContainer();
		NotifyIconContainer.MouseLeftButtonClick += OnMainWindowShowRequested;
		NotifyIconContainer.MouseRightButtonClick += OnMenuWindowShowRequested;
		WindowPainter = new WindowPainter();

		this._worker = worker;

		IsUpdating = new BooleanNotifier();
		IsWorking = new BooleanNotifier();

		ShowUpdatingTime(); // For debug
		ShowWorkingTime(); // For debug

		RushesRescan = new ReactiveProperty<bool>()
			.AddTo(this.Subscription);

		EngagesPriority = Settings.Current.ToReactivePropertyAsSynchronized(x => x.EngagesPriority)
			.AddTo(this.Subscription);
		EngagesPriority
			.Subscribe(_ => SetNotifyIconText())
			.AddTo(this.Subscription);

		#region Update

		RescanTimer = new ReactiveTimer(TimeSpan.FromSeconds(Settings.Current.RescanInterval))
			.AddTo(this.Subscription);

		RescanCommand = IsUpdating
			.Inverse()
			.ObserveOnUIDispatcher() // This is for thread access by ReactiveCommand.
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

				SetNotifyIconText();
			})
			.AddTo(this.Subscription);

		var networkRefreshed = Observable.FromEventPattern(
			h => _worker.NetworkRefreshed += h,
			h => _worker.NetworkRefreshed -= h);
		var availabilityChanged = Observable.FromEventPattern<AvailabilityChangedEventArgs>(
			h => _worker.AvailabilityChanged += h,
			h => _worker.AvailabilityChanged -= h);
		var interfaceChanged = Observable.FromEventPattern<InterfaceChangedEventArgs>(
			h => _worker.InterfaceChanged += h,
			h => _worker.InterfaceChanged -= h);
		var connectionChanged = Observable.FromEventPattern<ConnectionChangedEventArgs>(
			h => _worker.ConnectionChanged += h,
			h => _worker.ConnectionChanged -= h);
		var profileChanged = Observable.FromEventPattern<ProfileChangedEventArgs>(
			h => _worker.ProfileChanged += h,
			h => _worker.ProfileChanged -= h);
		Observable.Merge<object>(networkRefreshed, availabilityChanged, interfaceChanged, connectionChanged, profileChanged)
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
		await LanguageService.InitializeAsync();

		Settings.Current.Initiate();

		NotifyIconContainer.ShowIcon("pack://application:,,,/Resources/Icons/TrayIcon.ico", ProductInfo.Title);

		Profiles
			.ObserveElementProperty(x => x.IsConnected)
			.Throttle(TimeSpan.FromMilliseconds(100))
			.Subscribe(_ => NotifyIconContainer.Text = Profiles.Where(x => x.IsConnected).Aggregate(ProductInfo.Title, (w, n) => $"{w}{Environment.NewLine}{n.Name}"))
			.AddTo(this.Subscription);

		await LoadProfilesAsync();

		_current.MainWindow = new MainWindow(this);

		if (!StartupAgent.IsStartedOnSignIn())
			_current.MainWindow.Show();

		StartupAgent.HandleRequestAsync = HandleRequestAsync;

		Observable.FromEventPattern(
			h => _current.MainWindow.Activated += h,
			h => _current.MainWindow.Activated -= h)
			.StartWith(new object()) // This is for initial scan.
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
			WindowPainter.Dispose();
			Settings.Current.Dispose();
		}

		_disposed = true;

		base.Dispose(disposing);
	}

	#endregion

	protected Task<string> HandleRequestAsync(IReadOnlyCollection<string> args)
	{
		OnMainWindowShowRequested(null, EventArgs.Empty);
		return Task.FromResult<string>(null);
	}

	private void OnMainWindowShowRequested(object sender, EventArgs e)
	{
		_current.Dispatcher.Invoke(() => ShowMainWindow());
	}

	private void OnMenuWindowShowRequested(object sender, Point e)
	{
		ShowMenuWindow(e);
	}

	private void ShowMainWindow()
	{
		var window = (MainWindow)_current.MainWindow;
		if (window is null or { CanBeShown: false } or { Visibility: Visibility.Visible, IsForeground: true })
			return;

		window.Show();
		window.Activate();
	}

	private void ShowMenuWindow(Point pivot)
	{
		var window = new MenuWindow(this, pivot);
		window.Show();
	}

	private void SetNotifyIconText()
	{
		NotifyIconContainer.Text = ProductInfo.Title
			+ (RushesRescan.Value ? $"{Environment.NewLine}Rush {Settings.Current.RescanInterval}" : string.Empty)
			+ (EngagesPriority.Value ? $"{Environment.NewLine}Engage {Settings.Current.SignalThreshold}" : string.Empty);
	}

	#region Update

	private static readonly TimeSpan _scanTimeout = TimeSpan.FromSeconds(5);
	private static readonly TimeSpan _failureInterval = TimeSpan.FromMinutes(3);

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
				.ForEach(subProfiles =>
				{
					var count = subProfiles.Count();

					foreach (var profile in subProfiles)
						profile.PositionCount = count;
				});
		}

		Debug.WriteLine(Profiles.Any()
			? Profiles
				.Select(x => $"Profile {x.Name} -> AutoConnect: {x.IsAutoConnectEnabled}, AutoSwitch: {x.IsAutoSwitchEnabled}, Position: {x.Position}, IsRadioOn: {x.IsRadioOn}, Signal: {x.Signal}, IsConnected {x.IsConnected}")
				.Aggregate((w, n) => $"{w}{Environment.NewLine}{n}")
			: "No Profile");

		if (EngagesPriority.Value)
		{
			var targetProfiles = Profiles
				.GroupBy(x => x.InterfaceId)
				.Select(subProfiles =>
					(subProfiles.FirstOrDefault(x => x.IsConnected) switch
					{
						// If no profile is connected, select profiles that are automatic connection enabled.
						null => subProfiles.Where(x => x.IsAutoConnectEnabled),

						// If a profile is connected and it is automatic switch enabled, select profiles
						// that are automatic switch enabled including connected one.
						{ IsAutoSwitchEnabled: true } => subProfiles.Where(x => x.IsAutoSwitchEnabled),

						// If a profile is connected but it is not automatic switch enabled, leave as it is.
						_ => Enumerable.Empty<ProfileItem>()
					})
					.Where(x => Settings.Current.SignalThreshold <= x.Signal)
					// This interval is for the case where disconnection from wireless LAN is not immediately
					// reflected and take 2 to 3 minutes.
					.OrderBy(x => (x.LastFailureTime.Add(_failureInterval) > DateTime.Now))
					.ThenBy(x => x.Position)
					.FirstOrDefault())
				.Where(x => x is { IsConnected: false })
				.ToArray();

			if (targetProfiles.Length > 0)
			{
				await Task.WhenAll(targetProfiles.Select(async x =>
				{
					var result = await ConnectNetworkAsync(x, false);
					x.LastFailureTime = result ? default : DateTime.Now;
				}));
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
		Debug.WriteLine("ChangeOption start!");

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

	public bool IsUsableProfileName(Guid interfaceId, string profileName)
	{
		if (string.IsNullOrWhiteSpace(profileName))
			return false;

		return Profiles
			.Where(x => x.InterfaceId == interfaceId)
			.All(x => !string.Equals(profileName, x.Name, StringComparison.Ordinal));
	}

	public Task<bool> RenameProfileAsync(ProfileItem targetProfile, string profileName)
	{
		Debug.WriteLine("Rename start!");

		return WorkAsync(targetProfile, x => _worker.RenameProfileAsync(x, profileName));
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
		((targetProfile = Profiles.FirstOrDefault(x => x.IsTarget)) is not null);

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