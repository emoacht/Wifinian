using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ManagedNativeWifi;

namespace WlanProfileViewer.Models.Wlan
{
	internal class NativeWifiWorker : IWlanWorker
	{
		#region Get profiles

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync(bool isLatest, TimeSpan timeout)
		{
			if (isLatest)
				await NativeWifi.ScanNetworksAsync(timeout).ConfigureAwait(false);

			var profilePacks = await Task.Run(() => NativeWifi.EnumerateProfiles()).ConfigureAwait(false);

			return profilePacks.Select(x => new ProfileItem(
				name: x.Name,
				interfaceId: x.Interface.Id,
				interfaceName: null,
				interfaceDescription: x.Interface.Description,
				authentication: ConvertToAuthentication(x.Authentication),
				encryption: ConvertToEncryption(x.Encryption),
				position: x.Position,
				isAutomatic: x.IsAutomatic,
				signal: x.SignalQuality,
				isConnected: x.IsConnected));
		}

		private static AuthenticationMethod ConvertToAuthentication(string authenticationString)
		{
			switch (authenticationString)
			{
				case "open":
					return AuthenticationMethod.Open;
				case "shared":
					return AuthenticationMethod.Shared;
				case "WPA":
					return AuthenticationMethod.WPA_Enterprise;
				case "WPAPSK":
					return AuthenticationMethod.WPA_Personal;
				case "WPA2":
					return AuthenticationMethod.WPA2_Enterprise;
				case "WPA2PSK":
					return AuthenticationMethod.WPA2_Personal;
				default:
					return AuthenticationMethod.None;
			}
		}

		private static EncryptionType ConvertToEncryption(string encryptionString)
		{
			EncryptionType encryption;
			return Enum.TryParse(encryptionString, true, out encryption)
				? encryption
				: EncryptionType.None;
		}

		#endregion

		#region Set profile position

		public async Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));

			return await Task.Run(() => NativeWifi.SetProfilePosition(profileItem.InterfaceId, profileItem.Name, position));
		}

		#endregion

		#region Delete profile

		public async Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await Task.Run(() => NativeWifi.DeleteProfile(profileItem.InterfaceId, profileItem.Name));
		}

		#endregion

		#region Connect/Disconnect

		public async Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await NativeWifi.ConnectNetworkAsync(profileItem.InterfaceId, profileItem.Name, BssType.Any, timeout);
		}

		public async Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await NativeWifi.DisconnectNetworkAsync(profileItem.InterfaceId, timeout);
		}

		#endregion
	}
}