using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WlanProfileViewer.Models.Wlan
{
	internal class NetshWorker : IWlanWorker
	{
		#region Get profiles

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync(bool isLatest, TimeSpan timeout)
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
					   authentication: ConvertToAuthentication(profilePack.Authentication),
					   encryption: ConvertToEncryption(profilePack.Encryption),
					   position: profilePack.Position,
					   isAutomatic: profilePack.IsAutomatic,
					   signal: (networkPack?.Signal ?? 0),
					   isConnected: (interfacePack.IsConnected && profilePack.Name.Equals(interfacePack.ProfileName, StringComparison.Ordinal)));
		}

		private static AuthenticationMethod ConvertToAuthentication(string authenticationString)
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

		private static EncryptionType ConvertToEncryption(string encryptionString)
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

		#region Set profile position

		public async Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));

			return await Netsh.SetProfilePositionAync(profileItem.InterfaceName, profileItem.Name, position);
		}

		#endregion

		#region Delete profile

		public async Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await Netsh.DeleteProfileAsync(profileItem.InterfaceName, profileItem.Name);
		}

		#endregion

		#region Connect/Disconnect

		public async Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await Netsh.ConnectNetworkAsync(profileItem.InterfaceName, profileItem.Name);
		}

		public async Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			if (profileItem == null)
				throw new ArgumentNullException(nameof(profileItem));

			return await Netsh.DisconnectNetworkAsync(profileItem.InterfaceName);
		}

		#endregion
	}
}