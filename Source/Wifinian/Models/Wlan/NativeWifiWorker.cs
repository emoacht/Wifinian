using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using ManagedNativeWifi;

namespace Wifinian.Models.Wlan
{
	internal class NativeWifiWorker : IWlanWorker
	{
		private readonly NativeWifiPlayer _player;

		public NativeWifiWorker()
		{
			_player = new NativeWifiPlayer();
		}

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

			if (disposing)
			{
				_player.Dispose();
			}

			_disposed = true;
		}

		#endregion

		public event EventHandler NetworkRefreshed
		{
			add => _player.NetworkRefreshed += value;
			remove => _player.NetworkRefreshed -= value;
		}

		public event EventHandler InterfaceChanged
		{
			add => _player.InterfaceChanged += value;
			remove => _player.InterfaceChanged -= value;
		}

		public event EventHandler ConnectionChanged
		{
			add => _player.ConnectionChanged += value;
			remove => _player.ConnectionChanged -= value;
		}

		public event EventHandler ProfileChanged
		{
			add => _player.ProfileChanged += value;
			remove => _player.ProfileChanged -= value;
		}

		#region Scan networks

		public Task ScanNetworkAsync(TimeSpan timeout)
		{
			return _player.ScanNetworksAsync(timeout, CancellationToken.None);
		}

		#endregion

		#region Get profiles

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync()
		{
			var profilePacks = await Task.Run(() => _player.EnumerateProfiles()).ConfigureAwait(false);

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

		public Task<bool> SetProfileParameterAsync(ProfileItem profileItem)
		{
			if (!(profileItem is NativeWifiProfileItem item))
				throw new ArgumentException(nameof(profileItem));

			return Task.Run(() => _player.SetProfile(item.InterfaceId, item.ProfileType, item.Xml, null, true));
		}

		public Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position));

			return Task.Run(() => _player.SetProfilePosition(item.InterfaceId, item.Name, position));
		}

		#endregion

		#region Delete profile

		public Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			return Task.Run(() => _player.DeleteProfile(item.InterfaceId, item.Name));
		}

		#endregion

		#region Connect/Disconnect

		public Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			if (!(profileItem is NativeWifiProfileItem item))
				throw new ArgumentException(nameof(profileItem));

			return _player.ConnectNetworkAsync(item.InterfaceId, item.Name, item.BssType, timeout, CancellationToken.None);
		}

		public Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			return _player.DisconnectNetworkAsync(item.InterfaceId, timeout, CancellationToken.None);
		}

		#endregion
	}
}