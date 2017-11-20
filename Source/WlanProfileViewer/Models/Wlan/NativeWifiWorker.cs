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

			return profilePacks.Select(x => new NativeWifiProfileItem(
				name: x.Name,
				interfaceId: x.Interface.Id,
				interfaceDescription: x.Interface.Description,
				profileType: x.ProfileType,
				document: x.Document,
				position: x.Position,
				signal: x.SignalQuality,
				isConnected: x.IsConnected));
		}

		#endregion

		#region Set profile

		public async Task<bool> SetProfileParameterAsync(ProfileItem profileItem)
		{
			if (!(profileItem is NativeWifiProfileItem item))
				throw new ArgumentException(nameof(profileItem));

			return await Task.Run(() => NativeWifi.SetProfile(item.InterfaceId, item.ProfileType, item.Xml, null, true));
		}

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