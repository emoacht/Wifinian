using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;

namespace WiFiAdapterTest.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	private readonly List<WiFiAdapter> _adapters = [];

	public BindingList<NetworkViewModel> Networks { get; } = [];

	public NetworkViewModel? SelectedNetwork
	{
		get => _selectedNetwork;
		set => SetProperty(ref _selectedNetwork, value);
	}
	private NetworkViewModel? _selectedNetwork;

	public MainWindowViewModel()
	{
	}

	public async Task<bool> InitializeAsync()
	{
		var status = await WiFiAdapter.RequestAccessAsync();
		if (status is not WiFiAccessStatus.Allowed)
			return false;

		var collection = await DeviceInformation.FindAllAsync(WiFiAdapter.GetDeviceSelector());
		if (collection is not { Count: > 0 })
			return false;

		foreach (var id in collection.Select(x => x.Id))
		{
			var adapter = await WiFiAdapter.FromIdAsync(id);
			_adapters.Add(adapter);
		}
		return (_adapters.Count > 0);
	}

	public async Task UpdateNetworksAsync(bool rescan)
	{
		var networks = new List<NetworkViewModel>();

		foreach (var adapter in _adapters)
		{
			if (rescan)
				await adapter.ScanAsync();

			string? connectedSsid = null;
			bool isAdapterConnected = false;

			var connectedProfile = await adapter.NetworkAdapter.GetConnectedProfileAsync();
			if (connectedProfile is { IsWlanConnectionProfile: true, WlanConnectionProfileDetails: not null })
			{
				connectedSsid = connectedProfile.WlanConnectionProfileDetails.GetConnectedSsid();

				isAdapterConnected = (connectedProfile.GetNetworkConnectivityLevel() is not NetworkConnectivityLevel.None)
					&& !string.IsNullOrEmpty(connectedSsid);
			}

			foreach (var availableNetwork in adapter.NetworkReport.AvailableNetworks)
			{
				bool isNetworkConnected = isAdapterConnected
					&& string.Equals(availableNetwork.Ssid, connectedSsid, StringComparison.Ordinal);

				networks.Add(new NetworkViewModel(adapter.NetworkAdapter.NetworkAdapterId, availableNetwork, isNetworkConnected));
			}
		}

		Networks.Clear();

		foreach (var network in networks.OrderByDescending(x => x.Rssi))
			Networks.Add(network);
	}

	public async Task ConnectNetworkAsync()
	{
		if (SelectedNetwork is null)
			return;

		WiFiAdapter? adapter = _adapters.FirstOrDefault(x => x.NetworkAdapter.NetworkAdapterId == SelectedNetwork.AdapterId);
		if (adapter is null)
			return;

		WiFiConnectionResult result = await adapter.ConnectAsync(SelectedNetwork.Network, WiFiReconnectionKind.Manual);
		SelectedNetwork.IsConnected = (result.ConnectionStatus is WiFiConnectionStatus.Success);
	}

	public void DisconnectNetwork()
	{
		if (SelectedNetwork is null)
			return;

		WiFiAdapter? adapter = _adapters.FirstOrDefault(x => x.NetworkAdapter.NetworkAdapterId == SelectedNetwork.AdapterId);
		if (adapter is null)
			return;

		adapter.Disconnect();
		SelectedNetwork.IsConnected = false;
	}
}