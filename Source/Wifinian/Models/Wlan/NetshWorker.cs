using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ManagedNativeWifi;

namespace Wifinian.Models.Wlan
{
	internal class NetshWorker : IWlanWorker
	{
		public bool IsWorkable => true;

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

		#region Scan networks

		public async Task ScanNetworkAsync(TimeSpan timeout)
		{
			// Netsh has no function to directly prompt to scan wireless LANs.
			await DeferAsync(() =>
			{
				NetworkRefreshed?.Invoke(this, EventArgs.Empty);
				AvailabilityChanged?.Invoke(this, EventArgs.Empty);
				InterfaceChanged?.Invoke(this, EventArgs.Empty);
			});
		}

		#endregion

		#region Get profiles

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync()
		{
			var interfacePacks = (await Netsh.GetInterfacesAsync().ConfigureAwait(false))
				.ToArray(); // ToArray method is necessary.

			var networkPacks = (await Netsh.GetNetworksAsync().ConfigureAwait(false))
				.ToArray(); // ToArray method is necessary.

			var profilePacks = await Netsh.GetProfilesAsync().ConfigureAwait(false);

			return from profilePack in profilePacks
				   let networkPack = networkPacks.FirstOrDefault(x =>
					   x.InterfaceName.Equals(profilePack.InterfaceName, StringComparison.Ordinal) &&
					   x.Ssid.Equals(profilePack.Ssid, StringComparison.Ordinal))
				   from interfacePack in interfacePacks
				   where profilePack.InterfaceName.Equals(interfacePack.Name, StringComparison.Ordinal)
				   select new ProfileItem(
					   name: profilePack.Name,
					   interfaceId: interfacePack.Id,
					   interfaceName: profilePack.InterfaceName,
					   interfaceDescription: interfacePack.Description,
					   authentication: ConvertToAuthenticationMethod(profilePack.Authentication),
					   encryption: ConvertToEncryptionType(profilePack.Encryption),
					   isAutoConnectEnabled: profilePack.IsAutoConnectEnabled,
					   isAutoSwitchEnabled: profilePack.IsAutoSwitchEnabled,
					   position: profilePack.Position,
					   isRadioOn: interfacePack.IsRadioOn,
					   isConnected: (interfacePack.IsConnected && profilePack.Name.Equals(interfacePack.ProfileName, StringComparison.Ordinal)),
					   signal: (networkPack?.Signal ?? 0),
					   band: (networkPack?.Band ?? 0),
					   channel: (networkPack?.Channel ?? 0));
		}

		private static AuthenticationMethod ConvertToAuthenticationMethod(string authenticationString)
		{
			switch (authenticationString)
			{
				case "Open":
					return AuthenticationMethod.Open;
				case "Shared":
					return AuthenticationMethod.Shared;
				case "WPA-Enterprise":
					return AuthenticationMethod.WPA_Enterprise;
				case "WPA-Personal":
					return AuthenticationMethod.WPA_Personal;
				case "WPA2-Enterprise":
					return AuthenticationMethod.WPA2_Enterprise;
				case "WPA2-Personal":
					return AuthenticationMethod.WPA2_Personal;
				default:
					return AuthenticationMethod.None;
			}
		}

		private static EncryptionType ConvertToEncryptionType(string encryptionString)
		{
			switch (encryptionString)
			{
				case "WEP":
					return EncryptionType.WEP;
				case "TKIP":
					return EncryptionType.TKIP;
				case "CCMP":
					return EncryptionType.AES;
				default: // none
					return EncryptionType.None;
			}
		}

		#endregion

		#region Set/Rename/Delete profile

		public async Task<bool> SetProfileOptionAsync(ProfileItem profileItem)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (!await Netsh.SetProfileParameterAsync(item.InterfaceName, item.Name, item.IsAutoConnectEnabled, item.IsAutoSwitchEnabled))
				return false;

			await DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		public async Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position), "The position must not be negative.");

			if (!await Netsh.SetProfilePositionAsync(item.InterfaceName, item.Name, position))
				return false;

			await DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		public Task<bool> RenameProfileAsync(ProfileItem profileItem, string profileName)
		{
			// Netsh has no function to directly rename a wireless profile.
			return Task.FromResult(false);
		}

		public async Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (!await Netsh.DeleteProfileAsync(item.InterfaceName, item.Name))
				return false;

			await DeferAsync(() => ProfileChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		#endregion

		#region Connect/Disconnect

		public async Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (!await Netsh.ConnectNetworkAsync(item.InterfaceName, item.Name))
				return false;

			await DeferAsync(() => ConnectionChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		public async Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (!await Netsh.DisconnectNetworkAsync(item.InterfaceName))
				return false;

			await DeferAsync(() => ConnectionChanged?.Invoke(this, EventArgs.Empty));
			return true;
		}

		#endregion

		#region Base

		private Task DeferAsync(Action action)
		{
			return DeferAsync(action, TimeSpan.FromSeconds(1));
		}

		private async Task DeferAsync(Action action, TimeSpan deferDuration)
		{
			await Task.Delay(deferDuration).ConfigureAwait(false);
			action?.Invoke();
		}

		#endregion
	}
}