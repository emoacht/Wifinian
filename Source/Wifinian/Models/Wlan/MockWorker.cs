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
		public event EventHandler AvailabilityChanged;
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
				AvailabilityChanged?.Invoke(this, EventArgs.Empty);
				InterfaceChanged?.Invoke(this, EventArgs.Empty);
			});
		}

		public Task<IEnumerable<ProfileItem>> GetProfilesAsync()
		{
			if (_sourceProfiles == null)
				_sourceProfiles = PopulateProfiles().ToList();

			_sourceProfiles
				.ForEach(x =>
				{
					if (x.Signal > 0)
						x.Signal = Max(Min(x.Signal + _random.Value.Next(-10, 10), 100), 50);
				});

			return Task.FromResult(_sourceProfiles.AsEnumerable());
		}

		public Task<bool> SetProfileOptionAsync(ProfileItem profileItem)
		{
			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return Task.FromResult(false);

			targetProfile.IsAutoConnectEnabled = profileItem.IsAutoConnectEnabled;
			targetProfile.IsAutoSwitchEnabled = profileItem.IsAutoSwitchEnabled;

			deferTask = DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return Task.FromResult(true);
		}

		public Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			var targetProfiles = _sourceProfiles
				.Where(x => x.InterfaceId == profileItem.InterfaceId)
				.OrderBy(x => x.Position)
				.ToList();

			if ((targetProfiles.Count == 0) || (targetProfiles.Count - 1 < position))
				return Task.FromResult(false);

			var targetProfile = targetProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return Task.FromResult(false);

			if (targetProfile.Position == position)
				return Task.FromResult(false);

			targetProfiles.Remove(targetProfile);
			targetProfiles.Insert(position, targetProfile);

			int index = 0;
			targetProfiles.ForEach(x => x.Position = index++);

			deferTask = DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return Task.FromResult(true);
		}

		public Task<bool> RenameProfileAsync(ProfileItem profileItem, string profileName)
		{
			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return Task.FromResult(false);

			var renamedProfile = new ProfileItem(
				name: profileName,
				interfaceId: targetProfile.InterfaceId,
				interfaceName: targetProfile.InterfaceName,
				interfaceDescription: targetProfile.InterfaceDescription,
				authentication: targetProfile.Authentication,
				encryption: targetProfile.Encryption,
				isAutoConnectEnabled: targetProfile.IsAutoConnectEnabled,
				isAutoSwitchEnabled: targetProfile.IsAutoSwitchEnabled,
				position: targetProfile.Position,
				isRadioOn: targetProfile.IsRadioOn,
				signal: targetProfile.Signal,
				isConnected: targetProfile.IsConnected);

			_sourceProfiles.Remove(targetProfile);
			_sourceProfiles.Add(renamedProfile);

			deferTask = DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return Task.FromResult(true);
		}

		public Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			if (!_sourceProfiles.Remove(profileItem))
				return Task.FromResult(false);

			deferTask = DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return Task.FromResult(true);
		}

		public Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return Task.FromResult(false);

			targetProfile.IsConnected = true;

			deferTask = DeferAsync(() => ConnectionChanged?.Invoke(this, EventArgs.Empty));
			return Task.FromResult(true);
		}

		public Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return Task.FromResult(false);

			targetProfile.IsConnected = false;

			deferTask = DeferAsync(() => ConnectionChanged?.Invoke(this, EventArgs.Empty));
			return Task.FromResult(true);
		}

		#region Base

		private class InterfacePack
		{
			public Guid Id { get; }
			public string Name { get; }
			public string Description { get; }
			public bool IsRadioOn { get; }

			public InterfacePack(string name, string description, bool isRadioOn)
			{
				Id = Guid.NewGuid();
				this.Name = name;
				this.Description = description;
				this.IsRadioOn = isRadioOn;
			}
		}

		private ProfileItem[] PopulateProfiles()
		{
			var interfacePacks = new[]
			{
				new InterfacePack("Wi-Fi", "Intel(R) Centrino(R) Advanced-N 6205", true),
				new InterfacePack("Wi-Fi 2", "WLI-UC-GNM", true),
				new InterfacePack("Wi-Fi 3", "Marvel AVASTAR Wireless-AC Network Controller", false)
			};

			return new[]
			{
				new ProfileItem(
					name: "Cloud7",
					interfaceId: interfacePacks[0].Id,
					interfaceName: interfacePacks[0].Name,
					interfaceDescription: interfacePacks[0].Description,
					authentication: AuthenticationMethod.WPA_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: false,
					isAutoSwitchEnabled: false,
					position: 0,
					isRadioOn: interfacePacks[0].IsRadioOn,
					signal: 90,
					isConnected: false),

				new ProfileItem(
					name: "ESTACION",
					interfaceId: interfacePacks[0].Id,
					interfaceName: interfacePacks[0].Name,
					interfaceDescription: interfacePacks[0].Description,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 1,
					isRadioOn: interfacePacks[0].IsRadioOn,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "flashair_W02",
					interfaceId: interfacePacks[0].Id,
					interfaceName: interfacePacks[0].Name,
					interfaceDescription: interfacePacks[0].Description,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 2,
					isRadioOn: interfacePacks[0].IsRadioOn,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "flashair_W03",
					interfaceId: interfacePacks[0].Id,
					interfaceName: interfacePacks[0].Name,
					interfaceDescription: interfacePacks[0].Description,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: true,
					position: 3,
					isRadioOn: interfacePacks[0].IsRadioOn,
					signal: 90,
					isConnected: false),

				new ProfileItem(
					name: "Cloud7",
					interfaceId: interfacePacks[1].Id,
					interfaceName: interfacePacks[1].Name,
					interfaceDescription: interfacePacks[1].Description,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: false,
					isAutoSwitchEnabled: false,
					position: 0,
					isRadioOn: interfacePacks[1].IsRadioOn,
					signal: 70,
					isConnected: false),

				new ProfileItem(
					name: "nekoラン🐾🐾",
					interfaceId: interfacePacks[1].Id,
					interfaceName: interfacePacks[1].Name,
					interfaceDescription: interfacePacks[1].Description,
					authentication: AuthenticationMethod.WPA_Personal,
					encryption: EncryptionType.AES,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 1,
					isRadioOn: interfacePacks[1].IsRadioOn,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "La La Lan...",
					interfaceId: interfacePacks[2].Id,
					interfaceName: interfacePacks[2].Name,
					interfaceDescription: interfacePacks[2].Description,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					isAutoConnectEnabled: true,
					isAutoSwitchEnabled: false,
					position: 0,
					isRadioOn: interfacePacks[2].IsRadioOn,
					signal: 0,
					isConnected: false),
			};
		}

		private Task deferTask;

		private async Task DeferAsync(Action action)
		{
			await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
			action?.Invoke();
		}

		#endregion
	}
}