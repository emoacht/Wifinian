﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wifinian.Models.Wlan
{
	/// <summary>
	/// A wrapper class for Netsh commands
	/// </summary>
	/// <remarks>
	/// Basic Netsh commands for wireless LAN are described in:
	/// https://technet.microsoft.com/en-us/library/cc755301.aspx
	/// However, this is partly outdated. Check the latest information by Help command.
	/// </remarks>
	internal class Netsh
	{
		#region Type

		public enum NetworkType
		{
			None = 0,
			Infrastructure,
			Adhoc,
			Any
		}

		private class ShortProfilePack
		{
			public string Name { get; }
			public string InterfaceName { get; }
			public int Position { get; }

			public ShortProfilePack(string name, string interfaceName, int position)
			{
				this.Name = name;
				this.InterfaceName = interfaceName;
				this.Position = position;
			}
		}

		public class ProfilePack
		{
			public string Name { get; }
			public string InterfaceName { get; }
			public string Ssid { get; }
			public NetworkType NetworkType { get; }
			public string Authentication { get; }
			public string Encryption { get; }
			public bool IsAutoConnectEnabled { get; }
			public bool IsAutoSwitchEnabled { get; }
			public int Position { get; }

			public ProfilePack(
				string name,
				string interfaceName,
				string ssid,
				NetworkType networkType,
				string authentication,
				string encryption,
				bool isAutoConnectEnabled,
				bool isAutoSwitchEnabled,
				int position)
			{
				this.Name = name;
				this.InterfaceName = interfaceName;
				this.Ssid = ssid;
				this.NetworkType = networkType;
				this.Authentication = authentication;
				this.Encryption = encryption;
				this.IsAutoConnectEnabled = isAutoConnectEnabled;
				this.IsAutoSwitchEnabled = isAutoSwitchEnabled;
				this.Position = position;
			}
		}

		public class InterfacePack
		{
			public string Name { get; }
			public string Description { get; }
			public Guid Id { get; }
			public string PhysicalAddress { get; }
			public bool IsRadioOn { get; }
			public bool IsConnected { get; }
			public string ProfileName { get; }

			public InterfacePack(
				string name,
				string description,
				Guid id,
				string physicalAddress,
				bool isRadioOn,
				bool isConnected,
				string profileName)
			{
				this.Name = name;
				this.Description = description;
				this.Id = id;
				this.PhysicalAddress = physicalAddress;
				this.IsRadioOn = isRadioOn;
				this.IsConnected = isConnected;
				this.ProfileName = profileName;
			}
		}

		public class NetworkPack
		{
			public string InterfaceName { get; }
			public string Ssid { get; }
			public NetworkType NetworkType { get; }
			public string Authenticaion { get; }
			public string Encryption { get; }
			public string Protocol { get; }
			public int Signal { get; }
			public float Band { get; }
			public int Channel { get; }

			public NetworkPack(
				string interfaceName,
				string ssid,
				NetworkType networkType,
				string authentication,
				string encryption,
				string protocol,
				int signal,
				float band,
				int channel)
			{
				this.InterfaceName = interfaceName;
				this.Ssid = ssid;
				this.NetworkType = networkType;
				this.Authenticaion = authentication;
				this.Encryption = encryption;
				this.Protocol = protocol;
				this.Signal = signal;
				this.Band = band;
				this.Channel = channel;
			}
		}

		#endregion

		#region Get interfaces

		public static async Task<IEnumerable<InterfacePack>> GetInterfacesAsync()
		{
			var command = "netsh wlan show interfaces";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return EnumerateInterfaces(outputLines);
		}

		private static IEnumerable<InterfacePack> EnumerateInterfaces(IEnumerable<string> outputLines)
		{
			string name = null;
			string description = null;
			Guid id = Guid.Empty;
			string physicalAddress = null;
			bool? isConnected = null;
			string profileName = null;
			bool? isHardwareOn = null;
			bool? isSoftwareOn = null;

			foreach (var outputLine in outputLines)
			{
				try
				{
					if (name is null)
					{
						name = FindElement(outputLine, "Name");
						continue;
					}
					if (description is null)
					{
						description = FindElement(outputLine, "Description");
						continue;
					}
					if (id == Guid.Empty)
					{
						var buffer = FindElement(outputLine, "GUID");
						Guid.TryParse(buffer, out id); // If parsing failed, out variable will be Guid.Empty.
						continue;
					}
					if (physicalAddress is null)
					{
						physicalAddress = FindElement(outputLine, "Physical address");
						continue;
					}
					if (!isConnected.HasValue)
					{
						isConnected = FindElement(outputLine, "State").Equals("connected", StringComparison.Ordinal);
						continue;
					}
					if (isConnected.Value && (profileName is null))
					{
						profileName = FindElement(outputLine, "Profile");
						continue;
					}
					if (!isHardwareOn.HasValue)
					{
						isHardwareOn = FindBoolean(outputLine, "Hardware On", "Hardware Off");
						if (isHardwareOn.HasValue)
							continue;
					}
					if (!isSoftwareOn.HasValue)
					{
						isSoftwareOn = FindBoolean(outputLine, "Software On", "Software Off");
						if (isSoftwareOn.HasValue)
							continue;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Failed to enumerate interfaces." + Environment.NewLine
						+ ex);
					throw;
				}

				yield return new InterfacePack(
					name: name,
					description: description,
					id: id,
					physicalAddress: physicalAddress,
					isRadioOn: isConnected.Value || ((true == isHardwareOn) == isSoftwareOn),
					isConnected: isConnected.Value,
					profileName: profileName);

				name = null;
				description = null;
				id = Guid.Empty;
				physicalAddress = null;
				isConnected = null;
				profileName = null;
				isHardwareOn = null;
				isSoftwareOn = null;
			}
		}

		#endregion

		#region Get networks

		public static async Task<IEnumerable<NetworkPack>> GetNetworksAsync()
		{
			var command = "netsh wlan show networks mode=bssid";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return EnumerateNetworks(outputLines);
		}

		private static IEnumerable<NetworkPack> EnumerateNetworks(IEnumerable<string> outputLines)
		{
			var ssidPattern = new Regex(@"\bSSID (?<index>[1-9](?:\d|)(?:\d|)) *: *(?<value>\S.*)");
			var signalPattern = new Regex(@"\bSignal *: *(?<value>(?:100|[1-9](?:\d|)|0))%");
			var protocolPattern = new Regex(@"\bRadio type *: *802.11(?<value>[a-z]{1,2})");
			var channelPattern = new Regex(@"\bChannel *: *(?<value>[1-9]\d{0,2})");

			string interfaceName = null;
			string ssid = null;
			NetworkType networkType = default;
			string authentication = null;
			string encryption = null;
			int? signal = null;
			string protocol = null;
			int? channel = null;

			foreach (var outputLine in outputLines)
			{
				try
				{
					var buffer = FindElement(outputLine, "Interface name");
					if (buffer is not null)
					{
						interfaceName = buffer;
					}
					if (ssid is null)
					{
						if (!string.IsNullOrWhiteSpace(outputLine))
						{
							var match = ssidPattern.Match(outputLine);
							if (match.Success)
								ssid = match.Groups["value"].Value.Trim();
						}
						continue;
					}
					if (networkType == default)
					{
						networkType = ConvertToNetworkType(FindElement(outputLine, "Network type"));
						continue;
					}
					if (authentication is null)
					{
						authentication = FindElement(outputLine, "Authentication");
						continue;
					}
					if (encryption is null)
					{
						encryption = FindElement(outputLine, "Encryption");
						continue;
					}
					if (!signal.HasValue)
					{
						if (!string.IsNullOrWhiteSpace(outputLine))
						{
							var match = signalPattern.Match(outputLine);
							if (match.Success)
								signal = int.Parse(match.Groups["value"].Value);
						}
						continue;
					}
					if (protocol is null)
					{
						if (!string.IsNullOrWhiteSpace(outputLine))
						{
							var match = protocolPattern.Match(outputLine);
							if (match.Success)
								protocol = match.Groups["value"].Value;
						}
						continue;
					}
					if (!channel.HasValue)
					{
						if (!string.IsNullOrWhiteSpace(outputLine))
						{
							var match = channelPattern.Match(outputLine);
							if (match.Success)
								channel = int.Parse(match.Groups["value"].Value);
						}
						continue;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Failed to enumerate networks." + Environment.NewLine
						+ ex);
					throw;
				}

				yield return new NetworkPack(
					interfaceName: interfaceName,
					ssid: ssid,
					networkType: networkType,
					authentication: authentication,
					encryption: encryption,
					protocol: protocol,
					signal: signal.Value,
					band: (channel.Value <= 14) ? 2.4F : 5F,
					channel: channel.Value);

				ssid = null;
				networkType = default;
				authentication = null;
				encryption = null;
				signal = null;
				protocol = null;
				channel = null;
			}
		}

		#endregion

		#region Get profiles

		public static async Task<IEnumerable<ProfilePack>> GetProfilesAsync()
		{
			var command = "netsh wlan show profile";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			var shortProfiles = EnumerateShortProfiles(outputLines);

			return (await Task.WhenAll(shortProfiles.Select(async x => await GetProfileAsync(x.InterfaceName, x.Name, x.Position))))
				.Where(x => x is not null);
		}

		private static IEnumerable<ShortProfilePack> EnumerateShortProfiles(IEnumerable<string> outputLines)
		{
			var profilePattern = new Regex(@".*\bUser Profile\b *: *(?<value>\S.*)");

			const string profileHeader = "Profiles on interface";
			string interfaceName = null;
			int position = 0;

			foreach (var outputLine in outputLines.Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				if (outputLine.StartsWith(profileHeader, StringComparison.Ordinal))
				{
					interfaceName = outputLine.Substring(profileHeader.Length).Trim().TrimEnd(':');
					position = 0;
				}
				if (interfaceName is null)
					continue;

				var match = profilePattern.Match(outputLine);
				if (!match.Success)
					continue;

				var name = match.Groups["value"].Value.Trim();

				yield return new ShortProfilePack(name, interfaceName, position++);
			}
		}

		public static async Task<ProfilePack> GetProfileAsync(string interfaceName, string profileName, int position)
		{
			if (string.IsNullOrWhiteSpace(interfaceName))
				throw new ArgumentNullException(nameof(interfaceName));
			if (string.IsNullOrWhiteSpace(profileName))
				throw new ArgumentNullException(nameof(profileName));

			var command = $@"netsh wlan show profile name=""{profileName}"" interface=""{interfaceName}""";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return GetProfile(outputLines, interfaceName, profileName, position);
		}

		private static ProfilePack GetProfile(IEnumerable<string> outputLines, string interfaceName, string profileName, int position)
		{
			bool? autoConnect = null;
			bool? autoSwitch = null;
			string ssid = null;
			NetworkType networkType = default;
			string authentication = null;
			string encryption = null;

			foreach (var outputLine in outputLines.Where(x => !string.IsNullOrWhiteSpace(x)))
			{
				try
				{
					if (!autoConnect.HasValue)
					{
						var buffer = FindElement(outputLine, "Connection mode");
						if (buffer is not null)
							autoConnect = buffer.Equals("Connect automatically", StringComparison.OrdinalIgnoreCase);

						continue;
					}
					if (!autoSwitch.HasValue)
					{
						var buffer = FindElement(outputLine, "AutoSwitch");
						if (buffer is not null)
							autoSwitch = buffer.Equals("Switch to more preferred network if possible", StringComparison.OrdinalIgnoreCase);

						continue;
					}
					if (ssid is null)
					{
						var buffer = FindElement(outputLine, "SSID name");
						if (buffer is not null)
							ssid = buffer.Trim('"');

						continue;
					}
					if (networkType == default)
					{
						networkType = ConvertToNetworkType(FindElement(outputLine, "Network type"));
						continue;
					}
					if (authentication is null)
					{
						authentication = FindElement(outputLine, "Authentication");
						continue;
					}
					if (encryption is null)
					{
						encryption = FindElement(outputLine, "Cipher");
						break;
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine("Failed to enumerate profiles." + Environment.NewLine
						+ ex);
					throw;
				}
			}

			if (!autoConnect.HasValue ||
				!autoSwitch.HasValue ||
				(ssid is null) ||
				(networkType == default) ||
				(authentication is null) ||
				(encryption is null))
				return null;

			//Debug.WriteLine("Profile: {0}, Interface: {1}, SSID: {2}, BSS: {3}, Authentication: {4}, Encryption: {5}, AutoConnect: {6}, AutoSwitch: {7}, Position: {8}",
			//	profileName,
			//	interfaceName,
			//	ssid,
			//	networkType,
			//	authentication,
			//	encryption,
			//	autoConnect.Value,
			//	autoSwitch.Value,
			//	position);

			return new ProfilePack(
				name: profileName,
				interfaceName: interfaceName,
				ssid: ssid,
				networkType: networkType,
				authentication: authentication,
				encryption: encryption,
				isAutoConnectEnabled: autoConnect.Value,
				isAutoSwitchEnabled: autoSwitch.Value,
				position: position);
		}

		#endregion

		#region Set/Delete profile

		public static async Task<bool> SetProfileParameterAsync(string interfaceName, string profileName, bool isAutoConnectEnabled, bool isAutoSwitchEnabled)
		{
			if (string.IsNullOrWhiteSpace(interfaceName))
				throw new ArgumentNullException(nameof(interfaceName));
			if (string.IsNullOrWhiteSpace(profileName))
				throw new ArgumentNullException(nameof(profileName));

			var connectionMode = isAutoConnectEnabled ? "auto" : "manual";
			var autoSwitch = isAutoSwitchEnabled ? "yes" : "no";

			var command = $@"netsh wlan set profileparameter name=""{profileName}"" interface=""{interfaceName}"" ConnectionMode={connectionMode} autoSwitch={autoSwitch}";

			var expected = $@"Profile ""{profileName}"" on interface ""{interfaceName}"" updated successfully.";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return outputLines.Contains(expected);
		}

		public static async Task<bool> SetProfilePositionAsync(string interfaceName, string profileName, int position)
		{
			if (string.IsNullOrWhiteSpace(interfaceName))
				throw new ArgumentNullException(nameof(interfaceName));
			if (string.IsNullOrWhiteSpace(profileName))
				throw new ArgumentNullException(nameof(profileName));
			if (position < 0)
				throw new ArgumentOutOfRangeException(nameof(position), position, "The position must not be negative");

			position++; // According to the error message, "Profile preference order starts with 1."

			var command = $@"netsh wlan set profileorder name=""{profileName}"" interface=""{interfaceName}"" priority={position}";

			var expected = $@"Priority order of profile ""{profileName}"" is updated successfully.";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return outputLines.Contains(expected);
		}

		public static async Task<bool> DeleteProfileAsync(string interfaceName, string profileName)
		{
			if (string.IsNullOrWhiteSpace(profileName))
				throw new ArgumentNullException(nameof(profileName));

			var command = $@"netsh wlan delete profile name=""{profileName}""";
			if (!string.IsNullOrWhiteSpace(interfaceName))
				command += $@" interface=""{interfaceName}""";

			var expected = $@"Profile ""{profileName}"" is deleted";
			if (!string.IsNullOrWhiteSpace(interfaceName))
				expected += $@" from interface ""{interfaceName}"".";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return outputLines.Contains(expected);
		}

		#endregion

		#region Connect/Disconnect

		public static async Task<bool> ConnectNetworkAsync(string interfaceName, string profileName)
		{
			if (string.IsNullOrWhiteSpace(interfaceName))
				throw new ArgumentNullException(nameof(interfaceName));
			if (string.IsNullOrWhiteSpace(profileName))
				throw new ArgumentNullException(nameof(profileName));

			var command = $@"netsh wlan connect name=""{profileName}"" interface=""{interfaceName}""";

			var expected = "Connection request was completed successfully.";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return outputLines.Contains(expected);
		}

		public static async Task<bool> DisconnectNetworkAsync(string interfaceName)
		{
			if (string.IsNullOrWhiteSpace(interfaceName))
				throw new ArgumentNullException(nameof(interfaceName));

			var command = $@"netsh wlan disconnect interface=""{interfaceName}""";

			var expected = $@"Disconnection request was completed successfully for interface ""{interfaceName}"".";

			var outputLines = await ExecuteNetshAsync(command).ConfigureAwait(false);

			return outputLines.Contains(expected);
		}

		#endregion

		#region Helper

		private static string FindElement(string source, string elementName)
		{
			if (string.IsNullOrWhiteSpace(source))
				return null;

			int elementIndex = source.IndexOf(elementName, StringComparison.Ordinal);
			if (elementIndex < 0)
				return null;

			int delimiterIndex = source.IndexOf(':');
			if (delimiterIndex < elementIndex + elementName.Length)
				return null;

			return source.Substring(delimiterIndex + 1).Trim();
		}

		private static bool? FindBoolean(string source, string trueValue, string falseValue)
		{
			if (string.IsNullOrWhiteSpace(source))
				return null;

			if (0 <= source.IndexOf(trueValue, StringComparison.Ordinal))
				return true;

			if (0 <= source.IndexOf(falseValue, StringComparison.Ordinal))
				return false;

			return null;
		}

		private static NetworkType ConvertToNetworkType(string source)
		{
			if (string.IsNullOrWhiteSpace(source))
				return default;

			if (source.Equals("Infrastructure", StringComparison.OrdinalIgnoreCase))
				return NetworkType.Infrastructure;

			if (source.Equals("Adhoc", StringComparison.OrdinalIgnoreCase))
				return NetworkType.Adhoc;

			return NetworkType.Any;
		}

		#endregion

		#region Base

		private static async Task<string[]> ExecuteNetshAsync(string command)
		{
			string[] inputLines =
			{
				"chcp 437", // Change code page to 437 (US English) or 65001 (UTF-8).
				command,
				"exit",
			};

			using (var proc = new Process
			{
				StartInfo =
				{
					FileName = Environment.GetEnvironmentVariable("ComSpec"),
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardInput = true,
					RedirectStandardOutput = true
				},
				EnableRaisingEvents = true
			})
			{
				var outputLines = new List<string>();

				var tcs = new TaskCompletionSource<bool>();

				void received(object sender, DataReceivedEventArgs e) => outputLines.Add(e.Data);
				void exited(object sender, EventArgs e) => tcs.SetResult(true);

				try
				{
					proc.OutputDataReceived += received;
					proc.Exited += exited;

					proc.Start();
					proc.BeginOutputReadLine();

					using (var sw = proc.StandardInput)
					{
						if (sw.BaseStream.CanWrite)
						{
							foreach (var inputLine in inputLines)
								sw.WriteLine(inputLine);
						}
					}

					await tcs.Task;

					return outputLines.ToArray();
				}
				finally
				{
					proc.OutputDataReceived -= received;
					proc.Exited -= exited;
				}
			}
		}

		#endregion
	}
}