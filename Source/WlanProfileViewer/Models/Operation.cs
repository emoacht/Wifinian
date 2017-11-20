using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Helpers;
using Reactive.Bindings.Notifiers;

using WlanProfileViewer.Common;
using WlanProfileViewer.Models.Wlan;

namespace WlanProfileViewer.Models
{
	internal class Operation : DisposableBase
	{
		#region Constant

		private static readonly TimeSpan _loadingTimeoutDuration = TimeSpan.FromSeconds(10);

		private static readonly TimeSpan _workingTimeoutDuration = TimeSpan.FromSeconds(10);
		private static readonly TimeSpan _workingFirstInterval = TimeSpan.FromSeconds(0.1);
		private static readonly TimeSpan _workingSecondInterval = TimeSpan.FromSeconds(1);

		#endregion

		public ObservableCollection<ProfileItem> Profiles { get; } = new ObservableCollection<ProfileItem>();

		private readonly IWlanWorker _worker;

		public BooleanNotifier IsLoading { get; }
		public BooleanNotifier IsWorking { get; }

		public bool IsAutoReloadEnabled
		{
			get { return _isAutoReloadEnabled; }
			set { SetPropertyValue(ref _isAutoReloadEnabled, value); }
		}
		private bool _isAutoReloadEnabled;

		public bool IsSuspended
		{
			get { return _isSuspended; }
			set { SetPropertyValue(ref _isSuspended, value); }
		}
		private bool _isSuspended;

		private ReactiveTimer ReloadTimer { get; }

		public Operation() : this(new NativeWifiWorker())
		{ }

		public Operation(IWlanWorker worker)
		{
			this._worker = worker;

			IsLoading = new BooleanNotifier();
			IsWorking = new BooleanNotifier();

			ShowLoadingTime(); // For debug
			ShowWorkingTime(); // For debug

			ReloadTimer = new ReactiveTimer(TimeSpan.FromSeconds(Settings.Current.AutoReloadInterval))
				.AddTo(this.Subscription);
			ReloadTimer
				.Subscribe(async _ => await LoadProfilesAsync(true))
				.AddTo(this.Subscription);

			Settings.Current
				.ObserveProperty(x => x.AutoReloadInterval)
				.Throttle(TimeSpan.FromMilliseconds(100))
				.Subscribe(x => ReloadTimer.Interval = TimeSpan.FromSeconds(x))
				.AddTo(this.Subscription);

			this.ObserveProperty(x => x.IsAutoReloadEnabled)
				.Subscribe(isEnabled =>
				{
					if (isEnabled)
						ReloadTimer.Start();
					else
						ReloadTimer.Stop();
				})
				.AddTo(this.Subscription);

			this.ObserveProperty(x => x.IsSuspended, false)
				.Subscribe(async isSuspended =>
				{
					if (isSuspended)
					{
						ReloadTimer.Stop();
					}
					else
					{
						if (IsAutoReloadEnabled)
							ReloadTimer.Start();
						else
							await LoadProfilesAsync(false);
					}
				})
				.AddTo(this.Subscription);

			var initialTask = LoadProfilesAsync(false);
		}

		#region Load

		private readonly object _locker = new object();

		public async Task LoadProfilesAsync(bool isLatest)
		{
			lock (_locker)
			{
				if (IsLoading.Value)
					return;

				IsLoading.TurnOn();
			}

			Debug.WriteLine("Load start!");

			try
			{
				var oldProfileIndices = Enumerable.Range(0, Profiles.Count).Reverse().ToList(); // Reverse method is to start removing from the tail.
				var newProfiles = new List<ProfileItem>();

				foreach (var newProfile in await _worker.GetProfilesAsync(isLatest, _loadingTimeoutDuration))
				{
					var isExisting = false;

					for (int index = 0; (index < Profiles.Count) && !isExisting; index++)
					{
						var oldProfile = Profiles[index];
						if (!oldProfile.Id.Equals(newProfile.Id, StringComparison.Ordinal))
							continue;

						// Copy changeable values.
						oldProfile.IsAutoConnectionEnabled = newProfile.IsAutoConnectionEnabled;
						oldProfile.IsAutoSwitchEnabled = newProfile.IsAutoSwitchEnabled;
						oldProfile.Position = newProfile.Position;
						oldProfile.Signal = newProfile.Signal;
						oldProfile.IsConnected = newProfile.IsConnected;

						oldProfileIndices.Remove(index);
						isExisting = true;
					}

					if (!isExisting)
						newProfiles.Add(newProfile);
				}

				oldProfileIndices.ForEach(x => Profiles.RemoveAt(x));
				newProfiles.ForEach(x => Profiles.Add(x));

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

				Debug.WriteLine(Profiles.Any()
					? Profiles
						.Select(x => $"Profile {x.Name} -> AutoConnection {x.IsAutoConnectionEnabled}, AutoSwitch {x.IsAutoSwitchEnabled}, Position: {x.Position}, Signal: {x.Signal}, IsConnected {x.IsConnected}")
						.Aggregate((work, next) => work + Environment.NewLine + next)
					: "No Profile");
			}
			finally
			{
				IsLoading.TurnOff();
			}
		}

		[Conditional("DEBUG")]
		private void ShowLoadingTime()
		{
			this.IsLoading
				.Select(x => new { Value = x, Ticks = DateTime.Now.Ticks })
				.Pairwise()
				.Where(x => x.OldItem.Value && !x.NewItem.Value)
				.Select(x => (x.NewItem.Ticks - x.OldItem.Ticks) / TimeSpan.TicksPerMillisecond)
				.Subscribe(x => Debug.WriteLine($"Loading Time: {x}"))
				.AddTo(this.Subscription);
		}

		#endregion

		#region Work

		public async Task<bool> MoveUpProfileAsync()
		{
			Debug.WriteLine("MoveUp start!");

			var targetProfile = Profiles.FirstOrDefault(x => x.IsTarget);
			if (targetProfile == null)
				return false;

			var oldPosition = targetProfile.Position;
			var newPosition = oldPosition - 1;
			if (newPosition < 0)
				return false;

			return await WorkAsync(
				x => _worker.SetProfilePositionAsync(x, newPosition),
				x => Profiles.Contains(x) && (x.Position < oldPosition),
				targetProfile);
		}

		public async Task<bool> MoveDownProfileAsync()
		{
			Debug.WriteLine("MoveDown start!");

			var targetProfile = Profiles.FirstOrDefault(x => x.IsTarget);
			if (targetProfile == null)
				return false;

			var oldPosition = targetProfile.Position;
			var newPosition = oldPosition + 1;
			if (newPosition > targetProfile.PositionCount - 1)
				return false;

			return await WorkAsync(
				x => _worker.SetProfilePositionAsync(x, newPosition),
				x => Profiles.Contains(x) && (x.Position > oldPosition),
				targetProfile);
		}

		public async Task<bool> DeleteProfileAsync()
		{
			Debug.WriteLine("Delete start!");

			return await WorkAsync(
				x => _worker.DeleteProfileAsync(x),
				x => !Profiles.Contains(x));
		}

		public async Task<bool> ConnectNetworkAsync()
		{
			Debug.WriteLine("Connect start!");

			return await WorkAsync(
				x => _worker.ConnectNetworkAsync(x, _workingTimeoutDuration),
				x => Profiles.Contains(x) && x.IsConnected);
		}

		public async Task<bool> DisconnectNetworkAsync()
		{
			Debug.WriteLine("Disconnect start!");

			return await WorkAsync(
				x => _worker.DisconnectNetworkAsync(x, _workingTimeoutDuration),
				x => Profiles.Contains(x) && !x.IsConnected);
		}

		private async Task<bool> WorkAsync(Func<ProfileItem, Task<bool>> perform, Func<ProfileItem, bool> judge, ProfileItem targetProfile = null)
		{
			var targetProfileCopy = targetProfile ?? Profiles.FirstOrDefault(x => x.IsTarget);
			if (targetProfileCopy == null)
				return false;

			IsWorking.TurnOn();
			try
			{
				var timeoutTime = DateTime.Now.Add(_workingTimeoutDuration);

				if (!await perform(targetProfileCopy))
					return false;

				await Task.Delay(_workingFirstInterval);

				using (var cts = new CancellationTokenSource())
				{
					while (timeoutTime > DateTime.Now)
					{
						try
						{
							await Task.WhenAll(
								Task.Run(async () =>
								{
									await LoadProfilesAsync(false);

									if (judge(targetProfileCopy))
										cts.Cancel();
								}),
								Task.Delay(_workingSecondInterval, cts.Token));
						}
						catch (TaskCanceledException)
						{
						}

						if (cts.IsCancellationRequested)
							return true;
					}
					return false;
				}
			}
			finally
			{
				IsWorking.TurnOff();
			}
		}

		[Conditional("DEBUG")]
		private void ShowWorkingTime()
		{
			this.IsWorking
				.Select(x => new { Value = x, Ticks = DateTime.Now.Ticks })
				.Pairwise()
				.Where(x => x.OldItem.Value && !x.NewItem.Value)
				.Select(x => (x.NewItem.Ticks - x.OldItem.Ticks) / TimeSpan.TicksPerMillisecond)
				.Subscribe(x => Debug.WriteLine($"Working Time: {x}"))
				.AddTo(this.Subscription);
		}

		#endregion
	}
}