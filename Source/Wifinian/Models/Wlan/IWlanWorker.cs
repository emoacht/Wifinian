﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wifinian.Models.Wlan
{
	internal interface IWlanWorker : IDisposable
	{
		event EventHandler NetworkRefreshed;
		event EventHandler InterfaceChanged;
		event EventHandler ConnectionChanged;
		event EventHandler ProfileChanged;

		Task ScanNetworkAsync(TimeSpan timeout);

		Task<IEnumerable<ProfileItem>> GetProfilesAsync();

		Task<bool> SetProfileParameterAsync(ProfileItem profileItem);
		Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position);
		Task<bool> DeleteProfileAsync(ProfileItem profileItem);

		Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout);
		Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout);
	}
}