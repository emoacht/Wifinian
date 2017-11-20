using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WlanProfileViewer.Models.Wlan
{
	internal interface IWlanWorker
	{
		Task<IEnumerable<ProfileItem>> GetProfilesAsync(bool isLatest, TimeSpan timeout);

		Task<bool> SetProfileParameterAsync(ProfileItem profileItem);
		Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position);
		Task<bool> DeleteProfileAsync(ProfileItem profileItem);

		Task<bool> ConnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout);
		Task<bool> DisconnectNetworkAsync(ProfileItem profileItem, TimeSpan timeout);
	}
}