using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WlanProfileViewer.Models.Wlan
{
	internal interface IWlanWorker
	{
		Task<IEnumerable<ProfileItem>> GetProfilesAsync(bool isLatest, TimeSpan timeoutDuration);

		Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position);
		Task<bool> DeleteProfileAsync(ProfileItem profileItem);

		Task<bool> ConnectAsync(ProfileItem profileItem, TimeSpan timeoutDuration);
		Task<bool> DisconnectAsync(ProfileItem profileItem, TimeSpan timeoutDuration);
	}
}