using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

using ManagedNativeWifi;

namespace Wifinian.Models.Wlan
{
	internal class MockWorker : IWlanWorker
	{
		#region Dispose

		private bool _disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;
		}

		#endregion

		public event EventHandler NetworkRefreshed;
		public event EventHandler InterfaceChanged;
		public event EventHandler ConnectionChanged;
		public event EventHandler ProfileChanged;

		private List<ProfileItem> _sourceProfiles;
		private static readonly Lazy<Random> _random = new Lazy<Random>(() => new Random());

		public async Task ScanNetworkAsync(TimeSpan timeout)
		{
			await Task.Delay(TimeSpan.FromSeconds(1)); // Dummy

			deferTask = DeferAsync(() =>
			{
				NetworkRefreshed?.Invoke(this, EventArgs.Empty);
				InterfaceChanged?.Invoke(this, EventArgs.Empty);
			});
		}

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync()
		{
			if (_sourceProfiles == null)
				_sourceProfiles = PopulateProfiles().ToList();

			await WaitAsync(); // Dummy

			_sourceProfiles
				.ForEach(x =>
				{
					if (x.Signal > 0)
						x.Signal = Max(Min(x.Signal + _random.Value.Next(-10, 10), 100), 50);
				});

			return _sourceProfiles.ToArray();
		}

		public async Task<bool> SetProfileParameterAsync(ProfileItem profileItem)
		{
			await WaitAsync(); // Dummy

			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return false;

			targetProfile.IsAutoConnectEnabled = profileItem.IsAutoConnectEnabled;
			targetProfile.IsAutoSwitchEnabled = profileItem.IsAutoSwitchEnabled;

			deferTask = DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		public async Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			await WaitAsync(); // Dummy

			var targetProfiles = _sourceProfiles
				.Where(x => x.InterfaceId == profileItem.InterfaceId)
				.OrderBy(x => x.Position)
				.ToList();

			if ((targetProfiles.Count == 0) || (targetProfiles.Count - 1 < position))
				return false;

			var targetProfile = targetProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return false;

			if (targetProfile.Position == position)
				return true;

			targetProfiles.Remove(targetProfile);
			targetProfiles.Insert(position, targetProfile);

			int index = 0;
			targetProfiles.ForEach(x => x.Position = index++);

			deferTask = DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		public async Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			await WaitAsync(); // Dummy

			if (!_sourceProfiles.Remove(profileItem))
				return false;

			deferTask = DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		public async Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			await WaitAsync(); // Dummy

			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return false;

			targetProfile.IsConnected = true;

			deferTask = DeferAsync(() => ConnectionChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		public async Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			await WaitAsync(); // Dummy

			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return false;

			targetProfile.IsConnected = false;

			deferTask = DeferAsync(() => ConnectionChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		#region Base

		private ProfileItem[] PopulateProfiles()
		{
			var interfaceGuid0 = Guid.NewGuid();
			var interfaceGuid1 = Guid.NewGuid();
			var interfaceGuid2 = Guid.NewGuid();

			var interfaceName0 = "Wi-Fi";
			var interfaceName1 = "Wi-Fi 2";
			var interfaceName2 = "Wi-Fi 3";

			var interfaceDescription0 = "Intel(R) Centrino(R) Advanced-N 6205";
			var interfaceDescription1 = "GW-USValue-EZ";
			var interfaceDescription2 = "WLI-UC-GNM";

			return new[]
			{
				new ProfileItem(
					name: "at_STATION_Wi2",
					interfaceId: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					isAutoConnectEnabled: false,
					isAutoSwitchEnabled: false,
					position: 0,
					signal: 90,
					isConnected: false),

				new ProfileItem(
					name: "MSFTOPEN",
					interfaceId: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 1,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "flashair_W02",
					interfaceId: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 2,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "flashair_W03",
					interfaceId: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: true,
					position: 3,
					signal: 90,
					isConnected: false),

				new ProfileItem(
					name: "WIFIGATE-968",
					interfaceId: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: true,
					position: 4,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "at_STATION_Wi2",
					interfaceId: interfaceGuid1,
					interfaceName: interfaceName1,
					interfaceDescription: interfaceDescription1,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					isAutoConnectEnabled: false,
					isAutoSwitchEnabled: false,
					position: 0,
					signal: 70,
					isConnected: false),

				new ProfileItem(
					name: "ねこラン🐾🐾",
					interfaceId: interfaceGuid1,
					interfaceName: interfaceName1,
					interfaceDescription: interfaceDescription1,
					authentication: AuthenticationMethod.WPA_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 1,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "La La Lan...",
					interfaceId: interfaceGuid2,
					interfaceName: interfaceName2,
					interfaceDescription: interfaceDescription2,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 0,
					signal: 0,
					isConnected: false),
			};
		}

		private Task WaitAsync() => Task.Delay(TimeSpan.FromMilliseconds(_random.Value.Next(0, 100)));

		private Task deferTask;

		private async Task DeferAsync(Action action)
		{
			await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
			action?.Invoke();
		}

		#endregion
	}
}