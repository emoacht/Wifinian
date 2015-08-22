using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WlanProfileViewer.Models.Wlan
{
	internal class NativeWifiWorker : IWlanWorker
	{
		#region Get profiles

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync(bool isLatest, TimeSpan timeoutDuration)
		{
			if (isLatest)
				await NativeWifi.ScanAsync(timeoutDuration).ConfigureAwait(false);

			var profilePacks = await Task.Run(() => NativeWifi.EnumerateProfiles()).ConfigureAwait(false);

			return profilePacks.Select(x => new ProfileItem(
				name: x.Name,
				interfaceGuid: x.InterfaceGuid,
				interfaceName: null,
				interfaceDescription: x.InterfaceDescription,
				authentication: ConvertToAuthentication(x.Authentication),
				encryption: ConvertToEncryption(x.Encryption),
				position: x.Position,
				isAutomatic: x.IsAutomatic,
				signal: x.Signal,
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

			return await Task.Run(() => NativeWifi.SetProfilePosition(profileItem.Name, profileItem.InterfaceGuid, position));
		}

		#endregion

		#region Delete profile

		public async Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await Task.Run(() => NativeWifi.DeleteProfile(profileItem.Name, profileItem.InterfaceGuid));
		}

		#endregion

		#region Connect/Disconnect

		public async Task<bool> ConnectAsync(ProfileItem profileItem, TimeSpan timeoutDuration)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await NativeWifi.ConnectAsync(profileItem.Name, profileItem.InterfaceGuid, NativeWifi.BssType.Any, timeoutDuration);
		}

		public async Task<bool> DisconnectAsync(ProfileItem profileItem, TimeSpan timeoutDuration)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await NativeWifi.DisconnectAsync(profileItem.InterfaceGuid, timeoutDuration);
		}

		#endregion
	}
}