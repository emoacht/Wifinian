using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManagedNativeWifi;

namespace Wifinian.Models.Wlan;

internal interface IWlanWorker : IDisposable
{
	bool IsWorkable { get; }

	event EventHandler NetworkRefreshed;
	event EventHandler<AvailabilityChangedEventArgs> AvailabilityChanged;
	event EventHandler<InterfaceChangedEventArgs> InterfaceChanged;
	event EventHandler<ConnectionChangedEventArgs> ConnectionChanged;
	event EventHandler<ProfileChangedEventArgs> ProfileChanged;

	Task ScanNetworkAsync(TimeSpan timeout);

	Task<IEnumerable<ProfileItem>> GetProfilesAsync();

	Task<bool> SetProfileOptionAsync(ProfileItem profileItem);
	Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position);
	Task<bool> RenameProfileAsync(ProfileItem profileItem, string profileName);
	Task<bool> DeleteProfileAsync(ProfileItem profileItem);

	Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout);
	Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout);
}