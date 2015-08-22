using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace WlanProfileViewer.Models.Wlan
{
	internal class MockWorker : IWlanWorker
	{
		private List<ProfileItem> _sourceProfiles;
		private readonly Random _random = new Random();

		public async Task<IEnumerable<ProfileItem>> GetProfilesAsync(bool isLatest, TimeSpan timeoutDuration)
		{
			if (_sourceProfiles == null)
				_sourceProfiles = PopulateProfiles().ToList();

			await WaitAsync();

			_sourceProfiles
				.ForEach(x =>
				{
					if (x.Signal > 0)
						x.Signal = Max(Min(x.Signal + _random.Next(-10, 10), 100), 50);
				});

			return _sourceProfiles.ToArray();
		}

		public async Task<bool> SetProfilePositionAsync(ProfileItem profileItem, int position)
		{
			await WaitAsync();

			var targetProfiles = _sourceProfiles
				.Where(x => x.InterfaceGuid == profileItem.InterfaceGuid)
				.OrderBy(x => x.Position)
				.ToList();

			if ((targetProfiles.Count == 0) || (targetProfiles.Count - 1 < position))
				return false;

			var targetProfile = targetProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return false;

			if (targetProfile.Position == position)
				return true;

			targetProfiles.Remove(targetProfile);
			targetProfiles.Insert(position, targetProfile);

			int index = 0;
			targetProfiles.ForEach(x => x.Position = index++);

			return true;
		}

		public async Task<bool> DeleteProfileAsync(ProfileItem profileItem)
		{
			await WaitAsync();

			return _sourceProfiles.Remove(profileItem);
		}

		public async Task<bool> ConnectAsync(ProfileItem profileItem, TimeSpan timeoutDuration)
		{
			await WaitAsync();

			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return false;

			targetProfile.IsConnected = true;
			return true;
		}

		public async Task<bool> DisconnectAsync(ProfileItem profileItem, TimeSpan timeoutDuration)
		{
			await WaitAsync();

			var targetProfile = _sourceProfiles.FirstOrDefault(x => x.Id == profileItem.Id);
			if (targetProfile == null)
				return false;

			targetProfile.IsConnected = false;
			return true;
		}

		#region Base

		private IEnumerable<ProfileItem> PopulateProfiles()
		{
			var interfaceGuid0 = Guid.NewGuid();
			var interfaceGuid1 = Guid.NewGuid();
			var interfaceGuid2 = Guid.NewGuid();

			var interfaceName0 = "Wi-Fi";
			var interfaceName1 = "Wi-Fi 2";
			var interfaceName2 = "Wi-Fi 3";

			var interfaceDescription0 = "Intel(R) Centrino(R) Advanced-N 6205";
			var interfaceDescription1 = "GW-USValue-EZ";
			var interfaceDescription2 = "WLI-UC-GNM";

			return new[]
			{
				new ProfileItem(
					name: "at_STATION_Wi2",
					interfaceGuid: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					position: 0,
					isAutomatic: false,
					signal: 90,
					isConnected: false),

				new ProfileItem(
					name: "MSFTOPEN",
					interfaceGuid: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					position: 1,
					isAutomatic: true,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "flashair_W02",
					interfaceGuid: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					position: 2,
					isAutomatic: false,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "flashair_W03",
					interfaceGuid: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					position: 3,
					isAutomatic: false,
					signal: 90,
					isConnected: false),

				new ProfileItem(
					name: "WIFIGATE-968",
					interfaceGuid: interfaceGuid0,
					interfaceName: interfaceName0,
					interfaceDescription: interfaceDescription0,
					authentication: AuthenticationMethod.WPA2_Personal,
					encryption: EncryptionType.AES,
					position: 4,
					isAutomatic: false,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "at_STATION_Wi2",
					interfaceGuid: interfaceGuid1,
					interfaceName: interfaceName1,
					interfaceDescription: interfaceDescription1,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					position: 0,
					isAutomatic: false,
					signal: 70,
					isConnected: false),

				new ProfileItem(
					name: "ねこラン🐾🐾",
					interfaceGuid: interfaceGuid1,
					interfaceName: interfaceName1,
					interfaceDescription: interfaceDescription1,
					authentication: AuthenticationMethod.WPA_Personal,
					encryption: EncryptionType.AES,
					position: 1,
					isAutomatic: true,
					signal: 0,
					isConnected: false),

				new ProfileItem(
					name: "ZZZZZ...",
					interfaceGuid: interfaceGuid2,
					interfaceName: interfaceName2,
					interfaceDescription: interfaceDescription2,
					authentication: AuthenticationMethod.Open,
					encryption: EncryptionType.None,
					position: 0,
					isAutomatic: false,
					signal: 0,
					isConnected: false),
			};
		}

		private async Task WaitAsync()
		{
			await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(0, 100)));
		}

		#endregion
	}
}