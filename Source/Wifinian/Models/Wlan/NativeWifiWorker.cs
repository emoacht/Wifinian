using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			try
			{
				_player = new NativeWifiPlayer();
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to start NativeWifiPlayer." + Environment.NewLine
					+ ex);
			}
		}

		public bool IsWorkable => (_player is not null);

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
				_player?.Dispose();
			}

			_disposed = true;
		}

		#endregion

		public event EventHandler NetworkRefreshed
		{
			add { if (IsWorkable) { _player.NetworkRefreshed += value; } }
			remove { if (IsWorkable) { _player.NetworkRefreshed -= value; } }
		}

		public event EventHandler AvailabilityChanged
		{
			add { if (IsWorkable) { _player.AvailabilityChanged += value; } }
			remove { if (IsWorkable) { _player.AvailabilityChanged -= value; } }
		}

		public event EventHandler InterfaceChanged
		{
			add { if (IsWorkable) { _player.InterfaceChanged += value; } }
			remove { if (IsWorkable) { _player.InterfaceChanged -= value; } }
		}

		public event EventHandler ConnectionChanged
		{
			add { if (IsWorkable) { _player.ConnectionChanged += value; } }
			remove { if (IsWorkable) { _player.ConnectionChanged -= value; } }
		}

		public event EventHandler ProfileChanged
		{
			add { if (IsWorkable) { _player.ProfileChanged += value; } }
			remove { if (IsWorkable) { _player.ProfileChanged -= value; } }
		}

		#region Scan networks

		public Task ScanNetworkAsync(TimeSpan timeout)
		{
			if (!IsWorkable)
				return Task.CompletedTask;

			return _player.ScanNetworksAsync(timeout, CancellationToken.None);
		}

		#endregion

		#region Get profiles

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync()
		{
			if (!IsWorkable)
				return Array.Empty<NativeWifiProfileItem>();

			var profilePacks = await Task.Run(() => _player.EnumerateProfileRadios()).ConfigureAwait(false);

			return profilePacks.Select(x => new NativeWifiProfileItem(
				name: x.Name,
				interfaceId: x.Interface.Id,
				interfaceDescription: x.Interface.Description,
				profileType: x.ProfileType,
				document: x.Document,
				position: x.Position,
				isRadioOn: x.IsRadioOn,
				isConnected: x.IsConnected,
				signal: x.SignalQuality,
				band: x.Band,
				channel: x.Channel));
		}

		#endregion

		#region Set/Rename/Delete profile

		public Task<bool> SetProfileOptionAsync(ProfileItem profileItem)
		{
			if (profileItem is not NativeWifiProfileItem item)
				throw new ArgumentException(nameof(profileItem));

			if (!IsWorkable)
				return Task.FromResult(false);

			return Task.Run(() => _player.SetProfile(item.InterfaceId, item.ProfileType, item.Xml, null, true));
		}

		public Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position), position, "The position must not be negative.");

			if (!IsWorkable)
				return Task.FromResult(false);

			return Task.Run(() => _player.SetProfilePosition(item.InterfaceId, item.Name, position));
		}

		public Task<bool> RenameProfileAsync(ProfileItem profileItem, string profileName)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (string.IsNullOrWhiteSpace(profileName))
				throw new ArgumentNullException(nameof(profileName));

			if (!IsWorkable)
				return Task.FromResult(false);

			return Task.Run(() => _player.RenameProfile(item.InterfaceId, item.Name, profileName));
		}

		public Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (!IsWorkable)
				return Task.FromResult(false);

			return Task.Run(() => _player.DeleteProfile(item.InterfaceId, item.Name));
		}

		#endregion

		#region Connect/Disconnect

		public Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			if (profileItem is not NativeWifiProfileItem item)
				throw new ArgumentException(nameof(profileItem));

			if (!IsWorkable)
				return Task.FromResult(false);

			return _player.ConnectNetworkAsync(item.InterfaceId, item.Name, item.BssType, timeout, CancellationToken.None);
		}

		public Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout)
		{
			var item = profileItem ?? throw new ArgumentNullException(nameof(profileItem));

			if (!IsWorkable)
				return Task.FromResult(false);

			return _player.DisconnectNetworkAsync(item.InterfaceId, timeout, CancellationToken.None);
		}

		#endregion
	}
}