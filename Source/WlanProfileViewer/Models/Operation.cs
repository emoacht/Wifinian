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
		public ObservableCollection<ProfileItem> Profiles { get; } = new ObservableCollection<ProfileItem>();

		private readonly IWlanWorker _worker;

		public BooleanNotifier IsLoading { get; }
		public BooleanNotifier IsWorking { get; }

		public bool IsAutoRescanEnabled
		{
			get => _isAutoRescanEnabled;
			set => SetPropertyValue(ref _isAutoRescanEnabled, value);
		}
		private bool _isAutoRescanEnabled;

		public bool IsSuspended
		{
			get => _isSuspended;
			set => SetPropertyValue(ref _isSuspended, value);
		}
		private bool _isSuspended;

		private ReactiveTimer RescanTimer { get; }

		public Operation() : this(new NativeWifiWorker())
		{ }

		public Operation(IWlanWorker worker)
		{
			this._worker = worker;

			IsLoading = new BooleanNotifier();
			IsWorking = new BooleanNotifier();

			ShowLoadingTime(); // For debug
			ShowWorkingTime(); // For debug

			RescanTimer = new ReactiveTimer(TimeSpan.FromSeconds(Settings.Current.AutoRescanInterval))
				.AddTo(this.Subscription);
			RescanTimer
				.Subscribe(async _ => await ScanNetworkAsync())
				.AddTo(this.Subscription);

			Settings.Current
				.ObserveProperty(x => x.AutoRescanInterval)
				.Throttle(TimeSpan.FromMilliseconds(100))
				.Subscribe(x => RescanTimer.Interval = TimeSpan.FromSeconds(x))
				.AddTo(this.Subscription);

			this.ObserveProperty(x => x.IsAutoRescanEnabled)
				.Subscribe(isEnabled =>
				{
					if (isEnabled)
						RescanTimer.Start();
					else
						RescanTimer.Stop();
				})
				.AddTo(this.Subscription);

			this.ObserveProperty(x => x.IsSuspended, false)
				.Subscribe(async isSuspended =>
				{
					if (isSuspended)
					{
						RescanTimer.Stop();
					}
					else
					{
						if (IsAutoRescanEnabled)
							RescanTimer.Start();
						else
							await ScanNetworkAsync();
					}
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
				.Subscribe(async _ => await LoadProfilesAsync())
				.AddTo(this.Subscription);

			var loadTask = LoadProfilesAsync();
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
			}

			_disposed = true;

			base.Dispose(disposing);
		}

		#endregion

		#region Load

		private static readonly TimeSpan _scanTimeout = TimeSpan.FromSeconds(5);

		public async Task ScanNetworkAsync()
		{
			Debug.WriteLine("Scan start!");

			await _worker.ScanNetworkAsync(_scanTimeout);
		}

		private readonly object _locker = new object();

		public async Task LoadProfilesAsync()
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
					oldProfileIndices.Reverse(); // Reverse indices to start removing from the tail.
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
				}

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

		public async Task<bool> ChangeProfileParameterAsync()
		{
			Debug.WriteLine("ChangeParameter start!");

			return await WorkAsync(x => _worker.SetProfileParameterAsync(x));
		}

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

			return await WorkAsync(targetProfile, x => _worker.SetProfilePositionAsync(x, newPosition));
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

			return await WorkAsync(targetProfile, x => _worker.SetProfilePositionAsync(x, newPosition));
		}

		public async Task<bool> DeleteProfileAsync()
		{
			Debug.WriteLine("Delete start!");

			return await WorkAsync(x => _worker.DeleteProfileAsync(x));
		}

		public async Task<bool> ConnectNetworkAsync()
		{
			Debug.WriteLine("Connect start!");

			return await WorkAsync(x => _worker.ConnectNetworkAsync(x));
		}

		public async Task<bool> DisconnectNetworkAsync()
		{
			Debug.WriteLine("Disconnect start!");

			return await WorkAsync(x => _worker.DisconnectNetworkAsync(x));
		}

		private Task<bool> WorkAsync(Func<ProfileItem, Task<bool>> perform)
		{
			var targetProfile = Profiles.FirstOrDefault(x => x.IsTarget);
			if (targetProfile == null)
				return Task.FromResult(false);

			return WorkAsync(targetProfile, perform);
		}

		private async Task<bool> WorkAsync(ProfileItem targetProfile, Func<ProfileItem, Task<bool>> perform)
		{
			try
			{
				IsWorking.TurnOn();

				return await perform(targetProfile);
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